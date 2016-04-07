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

            var pingPong = new CountdownEvent(1000);

            var bruce = new Bruce(pingPong);
            var lee = new Lee(pingPong);
            system.Add(bruce, ctx => { bus = ctx.Actor.Bus = ctx.Bus; });
            system.Add(lee, ctx => { ctx.Actor.Bus = ctx.Bus; });
            system.Start();

            var p = new Pong();
            bus.Publish(ref p);

            var ended = pingPong.Wait(TimeSpan.FromSeconds(10));
            system.Stop();

            Assert.True(ended, "Bruce/Lee didn't ping/pong");
        }

        public class Bruce : IHandle<Ping>
        {
            private readonly CountdownEvent _pingPong;
            public IBus Bus;

            public Bruce(CountdownEvent pingPong)
            {
                _pingPong = pingPong;
            }

            public void Handle(ref Envelope envelope, ref Ping msg)
            {
                var p = new Pong();
                Bus.Publish(ref p);
                _pingPong.Signal();
            }
        }

        public class Lee : IHandle<Pong>
        {
            public IBus Bus;
            private readonly CountdownEvent _pingPong;

            public Lee(CountdownEvent pingPong)
            {
                _pingPong = pingPong;
            }

            public void Handle(ref Envelope envelope, ref Pong msg)
            {
                var p = new Ping();
                Bus.Publish(ref p);
                _pingPong.Signal();
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