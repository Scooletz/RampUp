using System;
using System.Threading;
using NUnit.Framework;
using RampUp.Actors;

namespace RampUp.Tests.Actors
{
    public class ActorSystemTests
    {
        [Test]
        public void BruceLeePingPong()
        {
            var system = new ActorSystem();
            IBus bus = null;
            var bruce = new Bruce();
            var lee = new Lee();
            system.Add(bruce, ctx => { bus = ctx.Actor.Bus = ctx.Bus; });
            system.Add(lee, ctx => { ctx.Actor.Bus = ctx.Bus; });
            system.Start();

            var p = new Pong();
            bus.Publish(ref p);

            Thread.Sleep(TimeSpan.FromSeconds(5));

            system.Stop();

            Assert.Less(0, bruce.Counter);
            Assert.Less(0, lee.Counter);

            Console.WriteLine($"Bruce ponged {bruce.Counter} times");
            Console.WriteLine($"Lee pinged {lee.Counter} times");
        }

        public class Bruce : IHandle<Ping>
        {
            public IBus Bus;
            public int Counter;

            public void Handle(ref Envelope envelope, ref Ping msg)
            {
                var p = new Pong();
                Bus.Publish(ref p);
                Counter += 1;
            }
        }

        public class Lee : IHandle<Pong>
        {
            public IBus Bus;
            public int Counter;

            public void Handle(ref Envelope envelope, ref Pong msg)
            {
                var p = new Ping();
                Bus.Publish(ref p);
                Counter += 1;
            }
        }

        public struct Pong : IMessage
        {
        }

        public struct Ping : IMessage
        {
        }
    }
}