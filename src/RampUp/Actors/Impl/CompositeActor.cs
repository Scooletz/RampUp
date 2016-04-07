using System;
using System.Linq;
using System.Runtime.CompilerServices;
using RampUp.Buffers;

namespace RampUp.Actors.Impl
{
    /// <summary>
    /// A simple implementation of composite actor, providing a common <see cref="MessageHandler"/> method for all composed actors
    /// </summary>
    public sealed class CompositeActor : IActor, IBatchAware
    {
        public readonly ActorDescriptor Descriptor;
        public const int MaxActors = sizeof (int);
        private const int MessageMapSize = 256;
        private const int MessageMapMask = MessageMapSize - 1;
        private readonly MessageReader[] _readers;
        private readonly int _count;
        private readonly int[] _messageMap;
        private readonly IBatchAware[] _batchAware;

        public CompositeActor(IActor[] actors, IStructSizeCounter counter, Func<Type, int> messageIdGetter)
        {
            if (actors.Length > MaxActors)
            {
                throw new ArgumentException($"Too many actors. Can composite only up to {MaxActors}");
            }

            var messageTypes = actors.Select(a => new ActorDescriptor(a))
                .SelectMany(descriptor => descriptor.HandledMessageTypes)
                .Distinct()
                .ToArray();

            Descriptor = new ActorDescriptor(messageTypes);
            _readers = actors.Select(a => new MessageReader(a, counter, messageIdGetter)).ToArray();
            _count = _readers.Length;

            _messageMap = BuildMessageMap(actors, messageIdGetter);
            _batchAware = actors.OfType<IBatchAware>().ToArray();
        }

        public void MessageHandler(int messageTypeId, ByteChunk chunk)
        {
            var hash = HashMessageId(messageTypeId);
            var actors = _messageMap[hash];

            for (var i = 0; i < _count; i++)
            {
                // invoke only if the mask matches the actors, to limit the overhead of calling unneeded actors
                if ((GetActorMask(i) & actors) != 0)
                {
                    _readers[i].MessageHandlerImpl(messageTypeId, chunk);
                }
            }
        }

        private static int[] BuildMessageMap(IActor[] actors, Func<Type, int> messageIdGetter)
        {
            var map = new int[MessageMapSize];

            for (var i = 0; i < actors.Length; i++)
            {
                var messageHashes = new ActorDescriptor(actors[i]).HandledMessageTypes
                    .Select(messageIdGetter)
                    .Select(HashMessageId)
                    .Distinct()
                    .ToArray();

                foreach (var messageHash in messageHashes)
                {
                    // this may collide because it's a hashtable, but it uses | so the cost of collision is running one actor additionally, if it happend at all
                    map[messageHash] |= GetActorMask(i);
                }
            }

            return map;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetActorMask(int actorIndex)
        {
            return 1 << actorIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int HashMessageId(int id)
        {
            // simplest hash, as the message ids are generated with ++ probably, this should have low collisions in majority of cases
            return id & MessageMapMask;
        }

        public void OnBatchEnded(ref BatchInfo info)
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _batchAware.Length; i++)
            {
                _batchAware[i].OnBatchEnded(ref info);
            }
        }
    }
}