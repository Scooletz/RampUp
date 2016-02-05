using System;
using NSubstitute;
using NUnit.Framework;
using RampUp.Actors;
using RampUp.Actors.Impl;
using RampUp.Ring;

namespace RampUp.Tests.Actors.Impl
{
    public abstract class ActorRegistryTestsBase
    {
        protected static readonly ActorId NoopId = new ActorId(1);
        protected readonly ActorId AId = new ActorId(2);
        protected readonly ActorId ABId = new ActorId(3);
        protected IRingBuffer NopBuffer;
        protected IRingBuffer ABuffer;
        protected IRingBuffer AbBuffer;
        protected ActorRegistry Registry;

        public class NopHandler : IActor
        {
        }

        public class AHandler : IHandle<A>
        {
            public void Handle(ref Envelope envelope, ref A msg)
            {
                throw new System.NotImplementedException();
            }
        }

        public struct A
        {
        }

        public class ABHandler : IHandle<A>, IHandle<B>
        {
            public void Handle(ref Envelope envelope, ref A msg)
            {
                throw new System.NotImplementedException();
            }

            public void Handle(ref Envelope envelope, ref B msg)
            {
                throw new System.NotImplementedException();
            }
        }

        public struct B
        {
        }

        [SetUp]
        public void SetUp()
        {
            NopBuffer = Substitute.For<IRingBuffer>();
            ABuffer = Substitute.For<IRingBuffer>();
            AbBuffer = Substitute.For<IRingBuffer>();

            Registry = new ActorRegistry(new[]
            {
                Tuple.Create(new ActorDescriptor(new NopHandler()), NopBuffer, NoopId),
                Tuple.Create(new ActorDescriptor(new AHandler()), ABuffer, AId),
                Tuple.Create(new ActorDescriptor(new ABHandler()), AbBuffer, ABId)
            });
        }
    }
}