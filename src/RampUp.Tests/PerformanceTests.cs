using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;
using RampUp.Actors;
using RampUp.Actors.Impl;
using RampUp.Buffers;
using RampUp.Ring;

namespace RampUp.Tests
{
    [Explicit]
    public class PerformanceTests
    {
        public struct A
        {
            public int Value;
        }

        public sealed class Handler : IHandle<A>
        {
            public void Handle(ref Envelope envelope, ref A msg)
            {
            }
        }

        [Test]
        public void Actors()
        {
            using (var buffer = new ManyToOneRingBuffer(new UnsafeBuffer(1024.Megabytes() + RingBufferDescriptor.TrailerLength)))
            {
                buffer.Write(5, new ByteChunk());
                buffer.Read((a, b) => { }, 1);

                var module = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("ActorsTestsDynAssembly"),
                    AssemblyBuilderAccess.Run).DefineDynamicModule("main");
                var counter = new StructSizeCounter();
                var registry = new ActorRegistry(new[] { Tuple.Create((IActor)new Handler(), (IRingBuffer)buffer, new ActorId(1)) });
                var writer = BaseMessageWriter.Build(counter, registry.GetMessageTypeId, new[] { typeof(A) }, module);

                var bus = new Bus(new ActorId(2), registry, 1, writer);

                var msg = new A();
                bus.Publish(ref msg);

                // publication is prepared do it
                var sw = Stopwatch.StartNew();
                var howMany = 20.Megabytes();
                for (var i = 0; i < howMany; i++)
                {
                    bus.Publish(ref msg);
                }
                Console.WriteLine($"{howMany} messages written in {sw.Elapsed}");
            }
        }
    }
}