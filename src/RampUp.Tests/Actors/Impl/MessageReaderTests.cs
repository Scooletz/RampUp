using System;
using NSubstitute;
using NUnit.Framework;
using RampUp.Actors;
using RampUp.Actors.Impl;
using RampUp.Buffers;

namespace RampUp.Tests.Actors.Impl
{
    public unsafe class MessageReaderTests
    {
        private const int ATypeId = 1;
        private const int BTypeId = 2;

        public class HandleNothing : IActor
        {
        }

        [Test]
        public void GivenEmptyActor_WhenAnyMessage_ThenPasses()
        {
            var counter = Substitute.For<IStructSizeCounter>();
            counter.GetSize(typeof(Envelope)).Returns(4);
            var reader = new MessageReader(new HandleNothing(), counter, t => { throw new Exception(); });

            var bytes = stackalloc byte[5];
            reader.MessageHandlerImpl(4, new ByteChunk(bytes, 5));
        }

        public struct A : IMessage
        {
        }

        public struct B : IMessage
        {
        }

        public class AHandler : IHandle<A>
        {
            public int Counter;
            public void Handle(ref Envelope envelope, ref A msg)
            {
                Counter += 1;
            }
        }

        [Test]
        public void AHandlerTests()
        {
            var counter = Substitute.For<IStructSizeCounter>();
            counter.GetSize(typeof(Envelope)).Returns(4);
            var aHandler = new AHandler();
            var reader = new MessageReader(aHandler, counter, GetABId);
            var bytes = stackalloc byte[10];
            var chunk = new ByteChunk(bytes, 10);

            Assert.AreEqual(0, aHandler.Counter);
            reader.MessageHandlerImpl(ATypeId, chunk);
            Assert.AreEqual(1, aHandler.Counter);
            reader.MessageHandlerImpl(BTypeId, chunk);
            Assert.AreEqual(1, aHandler.Counter);
        }

        public class ABHandler : IHandle<A>, IHandle<B>
        {
            public int CounterA;
            public int CounterB;

            public void Handle(ref Envelope envelope, ref A msg)
            {
                CounterA += 1;
            }

            public void Handle(ref Envelope envelope, ref B msg)
            {
                CounterB += 1;
            }
        }

        [Test]
        public void ABHandlerTests()
        {
            var counter = Substitute.For<IStructSizeCounter>();
            counter.GetSize(typeof(Envelope)).Returns(4);
            var h = new ABHandler();
            var reader = new MessageReader(h, counter, GetABId);
            var bytes = stackalloc byte[10];
            var chunk = new ByteChunk(bytes, 10);

            Assert.AreEqual(0, h.CounterA);
            Assert.AreEqual(0, h.CounterB);
            reader.MessageHandlerImpl(ATypeId, chunk);
            Assert.AreEqual(1, h.CounterA);
            Assert.AreEqual(0, h.CounterB);
            reader.MessageHandlerImpl(BTypeId, chunk);
            Assert.AreEqual(1, h.CounterA);
            Assert.AreEqual(1, h.CounterB);
        }

        private static int GetABId(Type messageType)
        {
            if (messageType == typeof(A))
            {
                return ATypeId;
            }
            if (messageType == typeof(B))
            {
                return BTypeId;
            }
            throw new Exception();
        }
    }
}