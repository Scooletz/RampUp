//using System;
//using System.Linq;
//using NSubstitute;
//using NUnit.Framework;
//using RampUp.Actors;
//using RampUp.Actors.Time.Impl;
//using RampUp.Buffers;

//namespace RampUp.Tests.Actors.Timer
//{
//    public class SchedulerTests
//    {
//        private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(7862345);
//        private const int SegmentLength = 4096;

//        [Test]
//        public unsafe void Test()
//        {
//            var actorId =new ActorId(34);
//            var bytes = stackalloc byte[SegmentLength];
//            var segment = new Segment(bytes, SegmentLength);

//            var pool = BuildSegmentPool(&segment);

//            var bus = Substitute.For<IBus>();

//            var message = Guid.NewGuid();
//            var called = false;
//            var scheduler = new Scheduler(actorId, bus, pool, new GuidMessageReaderWriter(), (msgId, chunk) =>
//            {
//                Assert.AreEqual(GuidMessageReaderWriter.MessageId, msgId);
//                Assert.AreEqual(GuidMessageReaderWriter.ChunkLength, chunk.Length);
//                Assert.AreEqual(message, *(Guid*) chunk.Pointer);

//                called = true;
//            });

//            // schedule timeut
//            scheduler.Schedule(Timeout, ref message);

//            var register = bus.ReceivedCalls().Single().GetArguments().OfType<Messages.RegisterTimeout>().Single();
//            Assert.AreEqual(Timeout, register.Timeout);

//            var e = new Envelope();
//            var timeout = new Messages.TimerTickOccured(register.Id);

//            // invoke timeout
//            scheduler.Handle(ref e, ref timeout);

//            Assert.True(called);
//        }

//        private static unsafe ISegmentPool BuildSegmentPool(Segment* segment)
//        {
//            return new OneSegmentOnlyPool(segment);
//        }
//    }
//}