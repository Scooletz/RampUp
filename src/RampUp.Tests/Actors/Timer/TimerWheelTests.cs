using System;
using NUnit.Framework;
using RampUp.Actors;
using RampUp.Actors.Time.Impl;
using RampUp.Buffers;

namespace RampUp.Tests.Actors.Timer
{
    public class TimerWheelTests
    {
        private const int InitialSegmentCount = 4096;
        private static readonly ActorId Actor = new ActorId(1);
        private static readonly TimeSpan TickLength = TimeSpan.FromMilliseconds(1);
        private readonly SingleThreadSegmentPool _pool = new SingleThreadSegmentPool(InitialSegmentCount);
        private long _ticks = 0;
        private GuidMessageReaderWriter _writer;
        private TimerWheel _wheel;
        private const long TicksPerSecond = 1000;

        [SetUp]
        public void SetUp()
        {
            _writer = new GuidMessageReaderWriter();
            _wheel = new TimerWheel(Actor, _pool, _writer, _writer.MessageHandler, TickLength, GetTicks, TicksPerSecond);
        }

        [TearDown]
        public void TearDown()
        {
            _wheel.Dispose();
            Assert.AreEqual(InitialSegmentCount, _pool.CountSegments());
            _ticks = 0;
        }

        [Test]
        public void WhenDisposedEmpty()
        {
        }

        [Test]
        public void WhenDisposedWithMessageScheduled_ShouldReturnAllSegments()
        {
            var guid = Guid.NewGuid();
            _wheel.Schedule(TickLength, ref guid);
        }

        [Test]
        public void WhenMessageScheduled_ThenItsDeliveredAfterTheTick()
        {
            var guid = Guid.NewGuid();
            _wheel.Schedule(TickLength, ref guid);

            Assert.False(_wheel.TryExpire());
            CollectionAssert.IsEmpty(_writer.Received);

            TickOnce();

            Assert.True(_wheel.TryExpire());
            CollectionAssert.AreEquivalent(new[] {guid}, _writer.Received);
        }

        [Test]
        public void WhenScheduledFarInTheFuture_ThenItsRoundedToMax()
        {
            var expire =
                TimeSpan.FromMilliseconds((TimerWheel.MaximumNumberOfIntervals + 1)*TickLength.TotalMilliseconds);
            var guid = Guid.NewGuid();
            _wheel.Schedule(expire, ref guid);

            for (int i = 0; i < TimerWheel.MaximumNumberOfIntervals; i++)
            {
                _wheel.TryExpire();
                CollectionAssert.IsEmpty(_writer.Received);
                TickOnce();
            }

            _wheel.TryExpire();
            CollectionAssert.AreEquivalent(new[] {guid}, _writer.Received);
        }

        private void TickOnce()
        {
            _ticks += 1;
        }

        private long GetTicks()
        {
            return _ticks;
        }
    }
}