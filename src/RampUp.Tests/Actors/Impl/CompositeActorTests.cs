using NUnit.Framework;
using RampUp.Actors;
using RampUp.Actors.Impl;
using RampUp.Buffers;
using AHandler = RampUp.Tests.Actors.Impl.ActorRegistryTestsBase.AHandler;
using ABHandler = RampUp.Tests.Actors.Impl.ActorRegistryTestsBase.ABHandler;
using A = RampUp.Tests.Actors.Impl.ActorRegistryTestsBase.A;
using B = RampUp.Tests.Actors.Impl.ActorRegistryTestsBase.B;

namespace RampUp.Tests.Actors.Impl
{
    public class CompositeActorTests
    {
        [Test]
        public void DesribesAndDispatchesProperly()
        {
            var a = new AHandler();
            var ab = new ABHandler();
            var actor = new CompositeActor(new IActor[] {a, ab}, new StructSizeCounter(),
                FakeMessageIdGenerator.GetMessageId);
            CollectionAssert.AreEquivalent(new[] {typeof (A), typeof (B)}, actor.Descriptor.HandledMessageTypes);
            var aid = FakeMessageIdGenerator.GetMessageId(typeof (A));
            var bid = FakeMessageIdGenerator.GetMessageId(typeof (B));

            // Ensure counters
            Assert.AreEqual(0, a.ACount);
            Assert.AreEqual(0, ab.ACount);
            Assert.AreEqual(0, ab.BCount);

            // A
            actor.MessageHandler(aid, ByteChunk.Empty);
            Assert.AreEqual(1, a.ACount);
            Assert.AreEqual(1, ab.ACount);
            Assert.AreEqual(0, ab.BCount);

            // B
            actor.MessageHandler(bid, ByteChunk.Empty);
            Assert.AreEqual(1, a.ACount);
            Assert.AreEqual(1, ab.ACount);
            Assert.AreEqual(1, ab.BCount);
        }
    }
}