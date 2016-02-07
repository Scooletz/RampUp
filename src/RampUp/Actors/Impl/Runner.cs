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
        private readonly IRingBuffer _buffer;
        private readonly int _batchSize;
        private readonly MessageHandler _handler;
        private readonly IBatchAware _batchAware;

        public Runner(IRingBuffer buffer, IStructSizeCounter counter, Func<Type, int> messageIdGetter, int batchSize,
            params IActor[] actors)
        {
            _buffer = buffer;
            _batchSize = batchSize;
            if (actors.Length > 1)
            {
                var actor = new CompositeActor(actors, counter, messageIdGetter);
                _batchAware = actor;
                _handler = actor.MessageHandler;
            }
            else
            {
                _handler = new MessageReader(actors[0], counter, messageIdGetter).MessageHandlerImpl;
                _batchAware = actors[0] as IBatchAware;
            }
        }

        public void SpinOnce(out BatchInfo info)
        {
            var start = Stopwatch.GetTimestamp();
            var processed = _buffer.Read(_handler, _batchSize);
            var stop = Stopwatch.GetTimestamp();

            info = new BatchInfo(_batchSize, processed, stop - start);

            _batchAware?.OnBatchEnded(ref info);
        }
    }
}