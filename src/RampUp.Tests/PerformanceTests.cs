using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Padded.Fody;
using RampUp.Actors;
using RampUp.Actors.Impl;
using RampUp.Buffers;
using RampUp.Ring;
using RampUp.Threading;

namespace RampUp.Tests
{
    [Explicit]
    public class PerformanceTests
    {
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

        [Test]
        public unsafe void Actors()
        {
            using (var buffer =
                new ManyToOneRingBuffer(new UnsafeBuffer(1024.Megabytes() + RingBufferDescriptor.TrailerLength)))
            {
                var someBytes = stackalloc byte[1];
                buffer.Write(5, new ByteChunk(someBytes, 1));
                buffer.Read((a, b) => { }, 1);

                var module = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("ActorsTestsDynAssembly"),
                    AssemblyBuilderAccess.Run).DefineDynamicModule("main");
                var counter = new StructSizeCounter();
                var registry =
                    new ActorRegistry(new[]
                    {Tuple.Create(new ActorDescriptor(new Handler()), (IRingBuffer) buffer, new ActorId(1))});
                var writer = MessageWriterBuilder.Build(counter, registry.GetMessageTypeId, new[] {typeof (A)}, module);

                var bus = new Bus(new ActorId(2), registry, 20, writer);

                var msg = new A();
                bus.Publish(ref msg);

                // publication is prepared do it

                var howMany = 200000000;
                var oneMiliion = 1000000;

                using (var scheduler = new RoundRobinThreadAffinedTaskScheduler(4))
                {
                    var wait = new ManualResetEventSlim();
                    var t = new CancellationToken();

                    var p1 = CreateProducer(wait, t, howMany, bus, scheduler);
                    var p2 = CreateProducer(wait, t, howMany, bus, scheduler);
                    var producentCount = 2;
                    var totalMessageCount = howMany*producentCount;

                    var worker = new Worker(totalMessageCount, wait, buffer, t);
                    var consumer = Task.Factory.StartNew(worker.DoWhile, t, TaskCreationOptions.LongRunning, scheduler);

                    GC.Collect(2, GCCollectionMode.Forced);

                    var sw = Stopwatch.StartNew();
                    wait.Set();

                    Task.WaitAll(p1, p2, consumer);

                    sw.Stop();
                    Console.WriteLine(
                        $"{howMany} messages written by each of {producentCount} producers and consumed in {sw.Elapsed}");
                    Console.WriteLine(
                        $"One million in {TimeSpan.FromMilliseconds(sw.Elapsed.TotalMilliseconds*oneMiliion/totalMessageCount)}");
                }
            }
        }

        private static Task CreateProducer(ManualResetEventSlim start, CancellationToken t, int howMany, Bus bus,
            RoundRobinThreadAffinedTaskScheduler scheduler)
        {
            return Task.Factory.StartNew(() =>
            {
                var a = new A();
                start.Wait(t);
                for (var i = 0; i < howMany; i++)
                {
                    bus.Publish(ref a);
                }
            }, t, TaskCreationOptions.LongRunning, scheduler);
        }

        [Padded]
        private sealed class Worker
        {
            private int _countDown;
            private readonly ManualResetEventSlim _wait;
            private readonly ManyToOneRingBuffer _buffer;
            private readonly CancellationToken _token;

            public Worker(int countDown, ManualResetEventSlim wait, ManyToOneRingBuffer buffer, CancellationToken token)
            {
                _countDown = countDown;
                _wait = wait;
                _buffer = buffer;
                _token = token;
            }

            public void DoWhile()
            {
                _wait.Wait(_token);

                while (_countDown > 0)
                {
                    _buffer.Read(Signal, 10000);
                }
            }

            private void Signal(int messagetypeid, ByteChunk chunk)
            {
                _countDown -= 1;
            }
        }
    }
}