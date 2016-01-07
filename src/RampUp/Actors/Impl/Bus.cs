using System;
using System.Collections.Generic;
using RampUp.Buffers;
using RampUp.Ring;

namespace RampUp.Actors.Impl
{
    public class Bus : IBus
    {
        private readonly ActorId _owner;
        private readonly IRingBuffer[] _buffers;
        private readonly IMessageWriter _sender;

        public Bus(ActorId owner, IRingBuffer[] buffers, IMessageWriter sender)
        {
            _owner = owner;
            _buffers = buffers;
            _sender = sender;
        }

        public void Publish<TMessage>(ref TMessage msg)
            where TMessage : struct
        {
            throw new System.NotImplementedException();
        }

        public void Send<TMessage>(ref TMessage msg, ActorId receiver)
            where TMessage : struct
        {
            var buffer = _buffers[receiver.Value];
            var envelope = new Envelope(_owner);

            _sender.Write(ref envelope, ref msg, buffer);
        }
    }
}