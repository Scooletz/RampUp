using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
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
            using (var buffer =
                new ManyToOneRingBuffer(new UnsafeBuffer(512.Megabytes() + RingBufferDescriptor.TrailerLength)))
            {
                buffer.Write(5, new ByteChunk());
                buffer.Read((a, b) => { }, 1);

                var module = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("ActorsTestsDynAssembly"),
                    AssemblyBuilderAccess.Run).DefineDynamicModule("main");
                var counter = new StructSizeCounter();
                var registry =
                    new ActorRegistry(new[] {Tuple.Create((IActor) new Handler(), (IRingBuffer) buffer, new ActorId(1))});
                var writer = BaseMessageWriter.Build(counter, registry.GetMessageTypeId, new[] {typeof (A)}, module);

                var bus = new Bus(new ActorId(2), registry, 1, writer);

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
                    var consumer = Task.Factory.StartNew(() =>
                    {
                        var c = 0;
                        wait.Wait(t);
                        while (c < totalMessageCount)
                        {
                            buffer.Read((msgdI, chunk) => { c++; }, 10000);
                        }
                    }, t, TaskCreationOptions.LongRunning, scheduler);

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
    }
}