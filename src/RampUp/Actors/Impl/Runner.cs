using System;
using System.Diagnostics;
using RampUp.Ring;

namespace RampUp.Actors.Impl
{
    /// <summary>
    /// A runner for a set of actors providing <see cref="SpinOnce"/> method for an actual thread/task.
    /// </summary>
    public sealed class Runner
    {
        public readonly IRingBuffer Buffer;
        public readonly ActorDescriptor Descriptor;
        private readonly int _batchSize;
        private readonly MessageHandler _handler;
        private readonly IBatchAware _batchAware;

        public Runner(IRingBuffer buffer, IStructSizeCounter counter, Func<Type, int> messageIdGetter, int batchSize,
            params IActor[] actors)
        {
            Buffer = buffer;
            _batchSize = batchSize;
            if (actors.Length > 1)
            {
                var actor = new CompositeActor(actors, counter, messageIdGetter);
                _batchAware = actor;
                _handler = actor.MessageHandler;
                Descriptor = actor.Descriptor;
            }
            else
            {
                _handler = new MessageReader(actors[0], counter, messageIdGetter).MessageHandlerImpl;
                Descriptor = new ActorDescriptor(actors[0]);
                _batchAware = actors[0] as IBatchAware;
            }
        }

        public void SpinOnce(out BatchInfo info)
        {
            var start = Stopwatch.GetTimestamp();
            var processed = Buffer.Read(_handler, _batchSize);
            var stop = Stopwatch.GetTimestamp();

            info = new BatchInfo(_batchSize, processed, stop - start);

            _batchAware?.OnBatchEnded(ref info);
        }
    }
}