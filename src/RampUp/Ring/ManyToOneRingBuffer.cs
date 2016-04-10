using System;
using Padded.Fody;
using RampUp.Atomics;
using RampUp.Buffers;
using static RampUp.Ring.RingBufferDescriptor;

namespace RampUp.Ring
{
    [Padded]
    public sealed class ManyToOneRingBuffer : IRingBuffer, IDisposable
    {
        private const int InsufficientCapacity = -1;
        private readonly IUnsafeBuffer _buffer;
        private readonly AtomicLong _headCache;
        private readonly AtomicLong _head;
        private readonly AtomicLong _tail;
        private readonly int _mask;

        public ManyToOneRingBuffer(IUnsafeBuffer buffer)
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

        public bool Write(int messageTypeId, ByteChunk chunk)
        {
            ValidateMessageTypeId(messageTypeId);
            ValidateLength(chunk);

            var isSuccessful = false;

            var recordLength = chunk.Length + HeaderLength;
            var requiredCapacity = recordLength.AlignToMultipleOf(RecordAlignment);
            var recordIndex = ClaimCapacity(requiredCapacity);

            if (InsufficientCapacity != recordIndex)
            {
                var index = _buffer.GetAtomicLong(recordIndex);
                index.VolatileWrite(MakeHeader(-recordLength, messageTypeId));
                var offset = EncodedMsgOffset(recordIndex);
                _buffer.Write(offset, chunk);
                _buffer.GetAtomicInt(recordIndex).VolatileWrite(recordLength);

                isSuccessful = true;
            }

            return isSuccessful;
        }

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
                        handler(messageTypeId, new ByteChunk(_buffer.RawBytes + recordIndex + HeaderLength, recordLength - HeaderLength));
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

        private int ClaimCapacity(int requiredCapacity)
        {
            var head = _headCache.VolatileRead();

            long tail;
            int tailIndex;
            int padding;
            do
            {
                tail = _tail.VolatileRead();
                var availableCapacity = Capacity - (int) (tail - head);

                if (requiredCapacity > availableCapacity)
                {
                    head = _head.VolatileRead();

                    if (requiredCapacity > Capacity - (int) (tail - head))
                    {
                        return InsufficientCapacity;
                    }

                    _headCache.VolatileWrite(head);
                }

                padding = 0;
                tailIndex = (int) tail & _mask;
                var toBufferEndLength = Capacity - tailIndex;

                if (requiredCapacity > toBufferEndLength)
                {
                    var headIndex = (int) head & _mask;

                    if (requiredCapacity > headIndex)
                    {
                        head = _head.VolatileRead();
                        headIndex = (int) head & _mask;
                        if (requiredCapacity > headIndex)
                        {
                            return InsufficientCapacity;
                        }

                        _headCache.VolatileWrite(head);
                    }

                    padding = toBufferEndLength;
                }
            } while (_tail.CompareExchange(tail + requiredCapacity + padding, tail) != tail);

            if (0 != padding)
            {
                var tailAtomic = _buffer.GetAtomicLong(tailIndex);
                tailAtomic.VolatileWrite(MakeHeader(padding, PaddingMsgTypeId));
                tailIndex = 0;
            }

            return tailIndex;
        }

        // ReSharper disable once UnusedParameter.Local
        private void ValidateLength(ByteChunk chunk)
        {
            if (chunk.Length > MaximumMessageLength)
            {
                throw new ArgumentException("");
            }
        }
    }
}