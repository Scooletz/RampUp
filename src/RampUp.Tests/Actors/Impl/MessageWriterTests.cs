using System;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using RampUp.Actors;
using RampUp.Actors.Impl;
using RampUp.Buffers;
using RampUp.Ring;

namespace RampUp.Tests.Actors.Impl
{
    public class MessageWriterTests
    {
        public struct A
        {
            public int Value;
        }

        [Test]
        public unsafe void Test()
        {
            var counter = Substitute.For<IStructSizeCounter>();
            const int aSize = 3;
            counter.GetSize(typeof (A)).Returns(aSize);
            const int envelopeSize = 4;
            counter.GetSize(typeof (Envelope)).Returns(envelopeSize);

            var buffer = Substitute.For<IRingBuffer>();
            buffer.Write(0, new ByteChunk()).ReturnsForAnyArgs(true);
            const int messageId = 5;
            var sender = new MessageWriter(counter, new[] {typeof (A)}, t=>messageId);
            var e = new Envelope(ActorId.Generate());
            var a = new A();

            Assert.True(sender.Write(ref e, ref a, buffer));

            var call = buffer.ReceivedCalls().Single();
            var args = call.GetArguments();
            Assert.AreEqual("Write", call.GetMethodInfo().Name);
            Assert.AreEqual(messageId, args[0]);
            Assert.AreEqual(new ByteChunk((byte*) &e, envelopeSize), args[1]);
            Assert.AreEqual(new ByteChunk((byte*) &a, aSize), args[2]);
        }
    }
}