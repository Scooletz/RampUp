using System;
using System.Threading;
using RampUp.Ring;

namespace RampUp.Actors.Impl
{
    public sealed class Bus : IBus
    {
        private readonly ActorId _owner;
        private readonly ActorRegistry _registry;
        private readonly int _throwAfterNTrials;
        private readonly IMessageWriter _writer;

        public Bus(ActorId owner, ActorRegistry registry, int throwAfterNTrials, IMessageWriter writer)
        {
            _owner = owner;
            _registry = registry;
            _throwAfterNTrials = throwAfterNTrials;
            _writer = writer;
        }

        public void Publish<TMessage>(ref TMessage msg)
            where TMessage : struct
        {
            var envelope = new Envelope(_owner);
            var buffers = _registry.GetBuffers(typeof(TMessage));
            for (var i = 0; i < buffers.Length; i++)
            {
                var buffer = buffers[i];
                Write(ref msg, envelope, buffer);
            }
        }

        public void Send<TMessage>(ref TMessage msg, ActorId receiver)
            where TMessage : struct
        {
            var buffer = _registry[receiver];
            var envelope = new Envelope(_owner);

            Write(ref msg, envelope, buffer);
        }

        public void SendToMe<TMessage>(ref TMessage msg) where TMessage : struct
        {
            var buffer = _registry[_owner];
            var envelope = new Envelope(_owner);

            Write(ref msg, envelope, buffer);
        }

        private void Write<TMessage>(ref TMessage msg, Envelope envelope, IRingBuffer buffer)
            where TMessage : struct
        {
            var successful = _writer.Write(ref envelope, ref msg, buffer.Write);
            if (successful == false)
            {
                var wait = new SpinWait();
                var counter = 1;
                do
                {
                    if (counter >= _throwAfterNTrials)
                    {
                        throw new Exception("Cannot proceed with a write");
                    }
                    counter += 1;
                    wait.SpinOnce();
                    successful = _writer.Write(ref envelope, ref msg, buffer.Write);
                } while (successful == false);
            }
        }
    }
}