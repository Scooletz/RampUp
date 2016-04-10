using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
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
        [StructLayout(LayoutKind.Sequential, Size = 64)]
        public struct A : IMessage
        {
            public int Value;
        }

        [Test]
        public unsafe void Test()
        {
            var counter = Substitute.For<IStructSizeCounter>();
            const int aSize = 64;
            counter.GetSize(typeof (A)).Returns(aSize);
            const int envelopeSize = 4;
            counter.GetSize(typeof (Envelope)).Returns(envelopeSize);

            var buffer = Substitute.For<IRingBuffer>();
            buffer.Write(0, new ByteChunk()).ReturnsForAnyArgs(true);
            const int messageId = 5;

            var asm = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("AnythingForTests"),
                AssemblyBuilderAccess.Run);
            var main = asm.DefineDynamicModule("main");
            var writer = BaseMessageWriter.Build(counter, l => messageId, new[] {typeof (A)}, main);

            var e = new Envelope(new ActorId(1));
            var a = new A();

            Assert.True(writer.Write(ref e, ref a, buffer));

            var call = buffer.ReceivedCalls().Single();
            var args = call.GetArguments();
            Assert.AreEqual("Write", call.GetMethodInfo().Name);
            Assert.AreEqual(messageId, args[0]);
            Assert.AreEqual(new ByteChunk((byte*) &a, aSize), args[1]);
        }
    }
}