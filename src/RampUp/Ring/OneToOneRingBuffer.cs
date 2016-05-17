using System;
using Padded.Fody;
using RampUp.Atomics;
using RampUp.Buffers;
using static RampUp.Ring.RingBufferDescriptor;

namespace RampUp.Ring
{
    [Padded]
    public sealed class OneToOneRingBuffer : IRingBuffer, IDisposable
    {
        /// <summary>
        /// Message type is padding to prevent fragmentation in the buffer
        /// </summary>
        public const int PaddingMsgTypeId = -1;

        private readonly IUnsafeBuffer _buffer;

        private readonly AtomicLong _headCache;

        private readonly AtomicLong _head;

        private readonly AtomicLong _tail;

        private readonly int _mask;

        public OneToOneRingBuffer(IUnsafeBuffer buffer)
        {
            _buffer = buffer;

            EnsureCapacity(buffer);
            Capacity = buffer.Size - TrailerLength;

            _mask = Capacity - 1;
            MaximumMessageLength = Capacity / 8;
            _tail = buffer.GetAtomicLong(Capacity + TailPositionOffset);
            _headCache = buffer.GetAtomicLong(Capacity + HeadCachePositionOffset);
            _head = buffer.GetAtomicLong(Capacity + HeadPositionOffset);
        }

        public void Dispose()
        {
            _buffer.Dispose();
        }

        public int Capacity { get; }

        public int MaximumMessageLength { get; }

        public int Read(MessageHandler handler, int messageProcessingLimit)
        {
            var messagesRead = 0;

            var head = _head.Read();

            var bytesRead = 0;

            var headIndex = (int)head & _mask;
            var contiguousBlockLength = Capacity - headIndex;

            try
            {
                while ((bytesRead < contiguousBlockLength) && (messagesRead < messageProcessingLimit))
                {
                    var recordIndex = headIndex + bytesRead;
                    var header = _buffer.GetAtomicLong(recordIndex).VolatileRead();

                    var recordLength = RecordLength(header);
                    if (recordLength <= 0)
                    {
                        break;
                    }

                    bytesRead += recordLength.AlignToMultipleOf(RecordAlignment);

                    var messageTypeId = MessageTypeId(header);
                    if (PaddingMsgTypeId == messageTypeId)
                    {
                        continue;
                    }

                    ++messagesRead;
                    unsafe
                    {
                        handler(messageTypeId,
                            new ByteChunk(_buffer.RawBytes + recordIndex + HeaderLength, recordLength - HeaderLength));
                    }
                }
            }
            finally
            {
                if (bytesRead != 0)
                {
                    _buffer.ZeroMemory(headIndex, bytesRead);
                    _head.VolatileWrite(head + bytesRead);
                }
            }

            return messagesRead;
        }

        public int ReadRaw(RawMessageChunkHandler handler, int maxSizeToProcess)
        {
            var head = _head.Read();

            var bytesRead = 0;

            var headIndex = (int)head & _mask;
            var maxLength = Math.Min(Capacity - headIndex, maxSizeToProcess);

            try
            {
                while (bytesRead < maxLength)
                {
                    var recordIndex = headIndex + bytesRead;
                    var header = _buffer.GetAtomicLong(recordIndex).VolatileRead();

                    var recordLength = RecordLength(header);
                    if (recordLength <= 0)
                    {
                        break;
                    }

                    bytesRead += recordLength.AlignToMultipleOf(RecordAlignment);

                    var messageTypeId = MessageTypeId(header);
                    if (PaddingMsgTypeId == messageTypeId)
                    {
                        break;
                    }
                }
                if (bytesRead > 0)
                {
                    unsafe
                    {
                        var chunk = new RawMessageChunk(new ByteChunk(_buffer.RawBytes + headIndex, bytesRead));
                        handler(ref chunk);
                    }
                }
            }
            finally
            {
                if (bytesRead != 0)
                {
                    _buffer.ZeroMemory(headIndex, bytesRead);
                    _head.VolatileWrite(head + bytesRead);
                }
            }

            return bytesRead;
        }

        public bool Write(int messageTypeId, ByteChunk chunk)
        {
            ValidateMessageTypeId(messageTypeId);
            ValidateLength(chunk);

            var recordLength = chunk.Length + HeaderLength;
            var requiredCapacity = recordLength.AlignToMultipleOf(RecordAlignment);

            var head = _headCache.Read();
            var tail = _tail.Read();
            var availableCapacity = Capacity - (int)(tail - head);

            if (requiredCapacity > availableCapacity)
            {
                head = _head.VolatileRead();

                if (requiredCapacity > Capacity - (int)(tail - head))
                {
                    return false;
                }

                _headCache.Write(head);
            }

            var padding = 0;
            var recordIndex = (int)tail & _mask;
            var toBufferEndLength = Capacity - recordIndex;

            if (requiredCapacity > toBufferEndLength)
            {
                var headIndex = (int)head & _mask;

                if (requiredCapacity > headIndex)
                {
                    head = _head.VolatileRead();
                    headIndex = (int)head & _mask;
                    if (requiredCapacity > headIndex)
                    {
                        return false;
                    }

                    _headCache.Write(head);
                }

                padding = toBufferEndLength;
            }

            if (0 != padding)
            {
                var tailAtomic = _buffer.GetAtomicLong(recordIndex);
                tailAtomic.VolatileWrite(MakeHeader(padding, PaddingMsgTypeId));
                recordIndex = 0;
            }

            var offset = EncodedMsgOffset(recordIndex);
            _buffer.Write(offset, chunk);

            var index = _buffer.GetAtomicLong(recordIndex);
            index.VolatileWrite(MakeHeader(recordLength, messageTypeId));

            _tail.VolatileWrite(tail + requiredCapacity + padding);

            return true;
        }

        private void ValidateLength(ByteChunk chunk)
        {
            if (chunk.Length > MaximumMessageLength)
            {
                throw new ArgumentException("");
            }
        }
    }
}