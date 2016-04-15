using System;
using System.Threading;
using NUnit.Framework;
using RampUp.Actors;

namespace RampUp.Tests.Actors
{
    public class ActorSystemTests
    {
        private const int PingPongCount = 1000;
        private const int PingCount = PingPongCount/2;
        private const int PongCount = PingCount;

        [Test]
        public void BruceLeePingPong()
        {
            var system = new ActorSystem();
            IBus bus = null;

            var pingPong = new CountdownEvent(PingPongCount);

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
            private int _counter;
            public IBus Bus;

            public Bruce(CountdownEvent pingPong)
            {
                _pingPong = pingPong;
            }

            public void Handle(ref Envelope envelope, ref Ping msg)
            {
                if (_counter == PongCount)
                {
                    return;
                }

                var p = new Pong();
                Bus.Publish(ref p);

                _counter += 1;
                _pingPong.Signal();
            }
        }

        public class Lee : IHandle<Pong>
        {
            public IBus Bus;
            private readonly CountdownEvent _pingPong;
            private int _counter;

            public Lee(CountdownEvent pingPong)
            {
                _pingPong = pingPong;
            }

            public void Handle(ref Envelope envelope, ref Pong msg)
            {
                if (_counter == PongCount)
                {
                    return;
                }

                var p = new Ping();
                Bus.Publish(ref p);

                _counter += 1;
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