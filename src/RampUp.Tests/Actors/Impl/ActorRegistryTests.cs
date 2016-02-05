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
        public void MessageTypeIdsAreAssignedUniquelyAndContinouslyFromZero()
        {
            var messageTypes = new[] {typeof (A), typeof (B)};
            var ids = messageTypes.Select(t => Registry.GetMessageTypeId(t)).OrderBy(k => k);

            CollectionAssert.AreEqual(new[] {1, 2}, ids);
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
            var aBuffers = Registry.GetBuffers(typeof (A));
            var bBuffers = Registry.GetBuffers(typeof (B));

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