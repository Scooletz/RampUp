using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using RampUp.Actors.Impl;
using RampUp.Buffers;
using RampUp.Ring;

namespace RampUp.Actors.Time.Impl
{
    /// <summary>
    /// The servicing actor, being registered with the actor requiring timeouts.
    /// </summary>
    /// <remarks>
    /// Based somehow on the timing wheels: <see cref="http://www.cs.columbia.edu/~nahum/w6998/papers/ton97-timing-wheels.pdf"/>
    /// </remarks>
    public unsafe class TimerWheel : IScheduler, IDisposable
    {
        private const int SizeOfSegment = 8 /*sizeof(Segment*)*/;
        private const int TimeoutsPerSegment = SingleThreadSegmentPool.SegmentSize/SizeOfSegment;
        private const int TimeoutsPerSegmentMask = TimeoutsPerSegment - 1;

        private readonly Segment* _segment;
        private readonly ISegmentPool _pool;
        private readonly MessageHandler _handler;
        private readonly Func<long> _getTicks;
        private readonly Segment** _wheel;
        private readonly SegmentChainMessageStore _store;
        private readonly long _ticksPerMilisecond;
        private readonly long _startTime;
        private readonly long _clockTickDurationInStopWatch;
        private Envelope _envelope;
        private long _currentTick;

        public TimerWheel(ActorId thisActor, ISegmentPool pool, IMessageWriter writer, MessageHandler handler,
            TimeSpan tickLength, Func<long> getTicks, long ticksPerSecond)
        {
            _envelope = new Envelope(thisActor);
            _pool = pool;
            _handler = handler;
            _getTicks = getTicks;
            _store = new SegmentChainMessageStore(writer, pool);
            var segment = _pool.Pop();
            _wheel = (Segment**) segment->Buffer;
            _segment = segment;

            // ensure clean wheel
            for (var i = 0; i < TimeoutsPerSegment; i++)
            {
                _wheel[i] = null;
            }

            _ticksPerMilisecond = ticksPerSecond/1000;
            _clockTickDurationInStopWatch = (long) tickLength.TotalMilliseconds*_ticksPerMilisecond;
            _startTime = _getTicks();
        }

        public TimerWheel(ActorId thisActor, ISegmentPool pool, IMessageWriter writer, MessageHandler handler,
            TimeSpan tickLength)
            : this(thisActor, pool, writer, handler, tickLength, Stopwatch.GetTimestamp, Stopwatch.Frequency)
        {
        }

        public void Schedule<TMessage>(TimeSpan timeout, ref TMessage message) where TMessage : struct
        {
            var milliseconds = (int) timeout.TotalMilliseconds;
            var segmentDiff = CalculateWheelOffset(milliseconds);

            var index = (_currentTick + segmentDiff) & TimeoutsPerSegmentMask;
            _store.Write(ref _envelope, ref message, ref _wheel[index]);
        }

        private long CalculateWheelOffset(long milliseconds)
        {
            var segmentDiff = milliseconds/_clockTickDurationInStopWatch;

            // if it's overlapping put max -1 value.
            if (segmentDiff > TimeoutsPerSegmentMask)
            {
                segmentDiff = TimeoutsPerSegmentMask;
            }
            return segmentDiff;
        }

        private long GetTicks()
        {
            return _getTicks() - _startTime;
        }

        public TimeSpan GetDelayTillNextTick()
        {
            var deadline = _clockTickDurationInStopWatch*(_currentTick + 1);

            // ReSharper disable once PossibleLossOfFraction
            return TimeSpan.FromMilliseconds((deadline - GetTicks())/_ticksPerMilisecond);
        }

        public bool TryExpire()
        {
            var nextTick = _currentTick + 1;
            var deadline = _clockTickDurationInStopWatch*nextTick;
            var now = GetTicks();

            if (now >= deadline)
            {
                _store.Consume(_handler, ref _wheel[nextTick & TimeoutsPerSegmentMask]);
                _currentTick = nextTick;
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            _pool.Push(_segment);
        }
    }
}