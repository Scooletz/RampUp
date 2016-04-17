using System;
using System.Threading;
using Padded.Fody;
using RampUp.Ring;

namespace RampUp.Actors.Impl
{
    [Padded]
    public sealed class Bus : IBus
    {
        private ActorId _owner;
        private ActorRegistry _registry;
        private int _throwAfterNTrials;
        private IMessageWriter _writer;
        private bool _sealed;

        public Bus(ActorId owner, ActorRegistry registry, int throwAfterNTrials, IMessageWriter writer)
        {
            Init(owner, registry, throwAfterNTrials, writer);
        }

        internal void Init(ActorId owner, ActorRegistry registry, int throwAfterNTrials, IMessageWriter writer)
        {
            if (_sealed)
            {
                throw new InvalidOperationException("The bus has been already initialized");
            }
            _owner = owner;
            _registry = registry;
            _throwAfterNTrials = throwAfterNTrials;
            _writer = writer;
            _sealed = true;
        }

        internal Bus()
        {
        }

        public void Publish<TMessage>(ref TMessage msg)
            where TMessage : struct
        {
            var envelope = new Envelope(_owner);
            ArraySegment<IRingBuffer> buffers;
            _registry.GetBuffers(typeof(TMessage), out buffers);

            if (buffers.Count == 0)
            {
                throw new ArgumentException($"There's no handler registered for a message of type {typeof(TMessage)}");
            }

            var a = buffers.Array;
            var notGreaterThan = buffers.Count + buffers.Offset;
            for (var i = buffers.Offset; i < notGreaterThan; i++)
            {
                var buffer = a[i];
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
            var successful = _writer.Write(ref envelope, ref msg, buffer);
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
                    successful = _writer.Write(ref envelope, ref msg, buffer);
                } while (successful == false);
            }
        }
    }
}