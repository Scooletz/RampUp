using System;
using System.Reflection;
using System.Reflection.Emit;

using BenchmarkDotNet.Attributes;

using Padded.Fody;

using RampUp.Actors;
using RampUp.Actors.Impl;
using RampUp.Buffers;
using RampUp.Ring;

namespace RampUp.Benchmarks.Actors.Impl
{
    public class BusBenchmarks
    {
        private ManyToOneRingBuffer _buffer;
        private Bus _bus;

        [Setup]
        public unsafe void Setup()
        {
            _buffer = new ManyToOneRingBuffer(new UnsafeBuffer(1024.Megabytes() + RingBufferDescriptor.TrailerLength));
            var someBytes = stackalloc byte[1];
            _buffer.Write(5, new ByteChunk(someBytes, 1));
            _buffer.Read((a, b) => { }, 1);

            var module = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("ActorsTestsDynAssembly"),
                AssemblyBuilderAccess.Run).DefineDynamicModule("main");
            var counter = new StructSizeCounter();
            var registry =
                new ActorRegistry(
                    new[]
                        { Tuple.Create(new ActorDescriptor(new Handler()), (IRingBuffer)_buffer, new ActorId(1)) });
            var writer = BaseMessageWriter.Build(counter, registry.GetMessageTypeId, new[] { typeof(A) }, module);

            _bus = new Bus(new ActorId(2), registry, 20, writer);

        }

        [Benchmark]
        public void BusPublish()
        {
            var msg = new A();
            _bus.Publish(ref msg);
        }

        public struct A : IMessage
        {
            public int Value;
        }

        [Padded]
        public sealed class Handler : IHandle<A>
        {
            public void Handle(ref Envelope envelope, ref A msg)
            {
            }
        }
    }
}