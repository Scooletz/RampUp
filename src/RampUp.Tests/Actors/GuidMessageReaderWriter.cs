using System;
using System.Collections.Generic;
using NUnit.Framework;
using RampUp.Actors;
using RampUp.Actors.Impl;
using RampUp.Buffers;

namespace RampUp.Tests.Actors
{
    public sealed class GuidMessageReaderWriter : IMessageWriter
    {
        public const int MessageId = 9873845;
        public const int ChunkLength = 16;

        public readonly List<Guid> Received = new List<Guid>();

        public unsafe bool Write<TMessage>(ref Envelope envelope, ref TMessage message, WriteDelegate write)
            where TMessage : struct
        {
            if (typeof (TMessage) != typeof (Guid))
            {
                throw new ArgumentException("Guids only!");
            }

            var guid = (Guid) ((object) message);

            var ch1 = new ByteChunk((byte*) &guid, 16);
            return write(MessageId, ch1);
        }

        public unsafe void MessageHandler(int messageTypeId, ByteChunk chunk)
        {
            Assert.AreEqual(MessageId, messageTypeId);
            Assert.AreEqual(ChunkLength, chunk.Length);
            var guid = *(Guid*) chunk.Pointer;
            Received.Add(guid);
        }
    }
}