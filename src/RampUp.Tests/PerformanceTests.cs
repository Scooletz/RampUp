using System;
using System.Diagnostics;
using NUnit.Framework;
using RampUp.Actors;
using RampUp.Actors.Impl;
using RampUp.Buffers;
using RampUp.Ring;
using RampUp.Tests.Actors.Impl;

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

                var registry = new ActorRegistry(new[] { Tuple.Create((IActor)new Handler(), (IRingBuffer)buffer, new ActorId(1)) });
                var bus = new Bus(new ActorId(2), registry, 1, new MessageWriter(new StructSizeCounter(), new[] { typeof(A) }, registry.GetMessageTypeId));

                var msg = new A();
                bus.Publish(ref msg);

                // publication is prepared do it
                var sw = Stopwatch.StartNew();
                for (var i = 0; i < 10.Megabytes(); i++)
                {
                    bus.Publish(ref msg);
                }
                Console.WriteLine(sw.Elapsed);
            }
        }
    }
}