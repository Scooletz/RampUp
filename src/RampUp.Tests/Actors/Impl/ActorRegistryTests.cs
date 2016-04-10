using System;
using System.Linq;
using NUnit.Framework;
using RampUp.Actors;
using RampUp.Actors.Impl;
using RampUp.Ring;

namespace RampUp.Tests.Actors.Impl
{
    public class ActorRegistryTests : ActorRegistryTestsBase
    {
        [Test]
        public void MessageTypeIdsAreAssignedUniquely()
        {
            var messageTypes = new[] {typeof (A), typeof (B)};
            var ids = messageTypes.Select(t => Registry.GetMessageTypeId(t)).Distinct().Count();

            Assert.AreEqual(2, ids);
        }

        [Test]
        public void RingBuffersAreResolvedByActorId()
        {
            var buffers = new[] {AbBuffer, ABuffer, NopBuffer};
            var ids = new[] {ABId, AId, NoopId};

            CollectionAssert.AreEqual(buffers, ids.Select(id => Registry[id]));
        }

        [Test]
        public void BuffersAreResolvedPropertlyByMessageType()
        {
            ArraySegment<IRingBuffer> aBuffers;
            Registry.GetBuffers(typeof (A), out aBuffers);
            ArraySegment<IRingBuffer> bBuffers;
            Registry.GetBuffers(typeof (B), out bBuffers);

            CollectionAssert.AreEquivalent(new[] {ABuffer, AbBuffer}, aBuffers);
            CollectionAssert.AreEquivalent(new[] {AbBuffer}, bBuffers);
        }

        [Test]
        public void UselessRegistryThrows()
        {
            Assert.Throws<ArgumentException>(
                () => { new ActorRegistry(new Tuple<ActorDescriptor, IRingBuffer, ActorId>[0]); });
        }
    }
}