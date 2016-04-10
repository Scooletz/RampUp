using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Padded.Fody;
using RampUp.Ring;

namespace RampUp.Actors.Impl
{
    /// <summary>
    /// This class holds a registry of actors, aggregating them and precalculating different queries that are run against them.
    /// </summary>
    [Padded]
    public sealed class ActorRegistry
    {
        private readonly IntLookup<ArraySegment<IRingBuffer>> _messageTypeToBuffers;
        private readonly IRingBuffer[] _buffersByActor;
        private readonly long _messageTypeDiff;

        public ActorRegistry(IReadOnlyCollection<Tuple<ActorDescriptor, IRingBuffer, ActorId>> actors)
        {
            if (actors.Count > ActorId.MaxValue)
            {
                throw new ArgumentException("To many actors");
            }

            var messageTypesOrdered = actors.Select(t => t.Item1)
                .SelectMany(d => d.HandledMessageTypes)
                .Distinct()
                .OrderBy(t => t.TypeHandle.Value.ToInt64())
                .ToArray();

            if (messageTypesOrdered.Length == 0)
            {
                throw new ArgumentException("The handled messages set is empty. This is a useless registry");
            }

            _messageTypeDiff = messageTypesOrdered.First().TypeHandle.Value.ToInt64() - 1;

            var ringsGroupedByMessageId = actors.SelectMany(
                t =>
                    t.Item1.HandledMessageTypes.Select(
                        messageType => new {MessageTypeId = GetMessageTypeId(messageType), Buffer = t.Item2}))
                .GroupBy(a => a.MessageTypeId)
                .ToArray();

            var buffers = new List<IRingBuffer>();
            AddPadding(buffers);

            var count = ringsGroupedByMessageId.Length;
            var keys = new int[count];
            var values = new Tuple<int, int>[count];
            var index = 0;

            foreach (var g in ringsGroupedByMessageId)
            {
                keys[index] = g.Key;
                var start = buffers.Count;
                buffers.AddRange(g.Select(tuple => tuple.Buffer));
                var end = buffers.Count;
                var length = end - start;

                values[index] = Tuple.Create(start, length);
                index += 1;
            }

            AddPadding(buffers);

            // create one table
            var bufferArray = buffers.ToArray();
            var valuesArray =
                values.Select(tuple => new ArraySegment<IRingBuffer>(bufferArray, tuple.Item1, tuple.Item2)).ToArray();

            _messageTypeToBuffers = new IntLookup<ArraySegment<IRingBuffer>>(keys, valuesArray);

            // by actor
            var max = actors.Max(t => t.Item3.Value);
            _buffersByActor = new IRingBuffer[max + 1];

            foreach (var actor in actors)
            {
                _buffersByActor[actor.Item3.Value] = actor.Item2;
            }
        }

        private static void AddPadding(List<IRingBuffer> buffers)
        {
            for (var i = 0; i < Native.CacheLineSize/Native.SmallestPossibleObjectReferenceSize; i++)
            {
                buffers.Add(null);
            }
        }

        public int GetMessageTypeId(Type messageType)
        {
            return (int) (messageType.TypeHandle.Value.ToInt64() - _messageTypeDiff);
        }

        public void GetBuffers(Type messageType, out ArraySegment<IRingBuffer> buffers)
        {
            _messageTypeToBuffers.GetOrDefault(GetMessageTypeId(messageType), out buffers);
        }

        public IRingBuffer this[ActorId actor] => _buffersByActor[actor.Value];

        /// <summary>
        /// Gets all the methods handling messages, declared in <paramref name="handlerType"/>.
        /// </summary>
        internal static IEnumerable<MethodInfo> GetHandleMethods(Type handlerType)
        {
            return handlerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(mi =>
                {
                    var isHandle = mi.Name == "Handle" && mi.ReturnType == typeof (void);
                    var parameterTypes = mi.GetParameters().Select(p => p.ParameterType).ToArray();
                    return isHandle &&
                           parameterTypes.Length == 2 &&
                           parameterTypes[0] == typeof (Envelope).MakeByRefType() &&
                           parameterTypes[1].IsByRef &&
                           parameterTypes[1].GetElementType().IsValueType;
                });
        }
    }
}