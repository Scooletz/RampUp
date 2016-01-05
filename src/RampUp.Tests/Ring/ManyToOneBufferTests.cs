using System;
using NSubstitute;
using NUnit.Framework;
using RampUp.Atomics;
using RampUp.Buffers;
using RampUp.Ring;

using static RampUp.Ring.RingBufferDescriptor;

namespace RampUp.Tests.Ring
{
    public unsafe class ManyToOneBufferTests
    {
        private const int MessageTypeId = 7;
        private const int Capacity = 4096;
        private static readonly int TotalBufferLength = Capacity + TrailerLength;
        private static readonly int TailCounterIndex = Capacity + TailPositionOffset;
        private static readonly int HeadCounterIndex = Capacity + HeadPositionOffset;
        private static readonly int HeadCounterCacheIndex = Capacity + HeadCachePositionOffset;

        private IUnsafeBuffer _buffer;
        private ManyToOneRingBuffer _ringBuffer;
        private static readonly IntPtr CurrentSlot = new IntPtr(0);
        private static readonly IntPtr Tail = new IntPtr(TailCounterIndex);
        private static readonly IntPtr Head = new IntPtr(HeadCounterIndex);
        private static readonly IntPtr HeadCounterCache = new IntPtr(HeadCounterCacheIndex);
        private Mocks.IAtomicLong _atomicLong;
        private Mocks.IAtomicInt _atomicInt;

        [SetUp]
        public void SetUp()
        {
            _buffer = Substitute.For<IUnsafeBuffer>();
            _buffer.Size.Returns(TotalBufferLength);

            _buffer.GetAtomicLong(Arg.Any<long>()).Returns(ci => new AtomicLong((byte*)ci.Arg<long>()));
            _buffer.GetAtomicInt(Arg.Any<long>()).Returns(ci => new AtomicInt((byte*)ci.Arg<long>()));

            _atomicLong = Substitute.For<Mocks.IAtomicLong>();
            Mocks.AtomicLong = _atomicLong;
            _atomicInt = Substitute.For<Mocks.IAtomicInt>();
            Mocks.AtomicInt = _atomicInt;

            _ringBuffer = new ManyToOneRingBuffer(_buffer);
        }

        [Test]
        public void ShouldWriteToEmptyBuffer()
        {
            const int length = 8;
            var recordLength = length + HeaderLength;
            var alignedRecordLength = recordLength.AlignToMultipleOf(Native.CacheLineSize);

            const int headValue = 0;
            _atomicLong.VolatileRead(Arg.Is(Head)).Returns(headValue);
            const int tailValue = 0;
            _atomicLong.VolatileRead(Arg.Is(Tail)).Returns(tailValue);

            _atomicLong.CompareExchange(Arg.Is(Tail), Arg.Is(tailValue), Arg.Is(tailValue + alignedRecordLength)).Returns(tailValue);

            var block = stackalloc byte[100];

            var chunk = new ByteChunk(block, length);
            Assert.IsTrue(_ringBuffer.Write(MessageTypeId, chunk));

            Received.InOrder(() =>
            {
                _atomicLong.VolatileWrite(CurrentSlot, MakeHeader(-recordLength, MessageTypeId));
                _buffer.Write(EncodedMsgOffset(tailValue), chunk);
                _buffer.GetAtomicInt(tailValue);
                _atomicInt.VolatileWrite(CurrentSlot, recordLength);
            });
        }

        [Test]
        public void ShouldRejectWriteWhenInsufficientSpace()
        {
            const int length = 200;
            const long head = 0;
            var tail = head + (Capacity - (length - RecordAlignment).AlignToMultipleOf(RecordAlignment));

            _atomicLong.VolatileRead(Head).Returns(head);
            _atomicLong.VolatileRead(Tail).Returns(tail);

            var chunk = new ByteChunk(null, length);
            Assert.False(_ringBuffer.Write(MessageTypeId, chunk));

            _atomicInt.ReceivedWithAnyArgs(0);
            _atomicLong.DidNotReceiveWithAnyArgs().CompareExchange(IntPtr.Zero, 0, 0);
            _atomicLong.DidNotReceiveWithAnyArgs().VolatileWrite(IntPtr.Zero, 0);
            _buffer.DidNotReceiveWithAnyArgs().Write(0, new ByteChunk(null, 0));
        }

        [Test]
        public void ShouldRejectWriteWhenBufferFull()
        {
            const int length = 8;
            const long head = 0L;
            const long tail = head + Capacity;

            _atomicLong.VolatileRead(Head).Returns(head);
            _atomicLong.VolatileRead(Tail).Returns(tail);

            Assert.False(_ringBuffer.Write(MessageTypeId, new ByteChunk(null, length)));

            _atomicInt.ReceivedWithAnyArgs(0);
            _atomicLong.DidNotReceiveWithAnyArgs().CompareExchange(IntPtr.Zero, 0, 0);
            _atomicLong.DidNotReceiveWithAnyArgs().VolatileWrite(IntPtr.Zero, 0);
            _buffer.DidNotReceiveWithAnyArgs().Write(0, new ByteChunk(null, 0));
        }

        [Test]
        public void ShouldInsertPaddingRecordPlusMessageOnBufferWrap()
        {
            const int length = 200;
            var recordLength = length + HeaderLength;
            var alignedRecordLength = recordLength.AlignToMultipleOf(RecordAlignment);
            long tail = Capacity - HeaderLength;
            var head = tail - RecordAlignment * 4;

            _atomicLong.VolatileRead(Head).Returns(head);
            _atomicLong.VolatileRead(Tail).Returns(tail);
            _atomicLong.CompareExchange(Tail, tail + alignedRecordLength + RecordAlignment, tail).Returns(tail);

            var chunk = new ByteChunk(null, length);
            Assert.True(_ringBuffer.Write(MessageTypeId, chunk));

            Received.InOrder(() =>
            {
                // padding first
                _atomicLong.VolatileWrite(new IntPtr(tail), MakeHeader(HeaderLength, PaddingMsgTypeId));

                // then write from the start
                _atomicLong.VolatileWrite(new IntPtr(0), MakeHeader(-recordLength, MessageTypeId));
                _buffer.Write(EncodedMsgOffset(0), chunk);
                _buffer.GetAtomicInt(0);
                _atomicInt.VolatileWrite(new IntPtr(0), recordLength);
            });
        }

        [Test]
        public void ShouldInsertPaddingRecordPlusMessageOnBufferWrapWithHeadEqualToTail()
        {
            const int length = 200;
            var recordLength = length + HeaderLength;
            var alignedRecordLength = recordLength.AlignToMultipleOf(RecordAlignment);
            var tail = Capacity - HeaderLength;
            var head = tail;

            _atomicLong.VolatileRead(Head).Returns(head);
            _atomicLong.VolatileRead(Tail).Returns(tail);
            _atomicLong.CompareExchange(Tail, tail + alignedRecordLength + RecordAlignment, tail).Returns(tail);

            var chunk = new ByteChunk(null, length);
            Assert.IsTrue(_ringBuffer.Write(MessageTypeId, chunk));

            Received.InOrder(() =>
            {
                // padding first
                _atomicLong.VolatileWrite(new IntPtr(tail), MakeHeader(HeaderLength, PaddingMsgTypeId));

                // message then
                _atomicLong.VolatileWrite(new IntPtr(0), MakeHeader(-recordLength, MessageTypeId));
                _buffer.Write(EncodedMsgOffset(0), chunk);
                _atomicInt.VolatileWrite(new IntPtr(0), recordLength);
            });
        }

        [Test]
        public void ShouldReadNothingFromEmptyBuffer()
        {
            const long head = 0L;
            _atomicLong.Read(Head).Returns(head);

            var handler = default(MessageHandler);
            var read = _ringBuffer.Read(handler, 100);

            Assert.AreEqual(0, read);
        }

        [Test]
        public void ShouldNotReadSingleMessagePartWayThroughWriting()
        {
            const long head = 0L;
            const int headIndex = (int)head;

            _atomicLong.Read(Head).Returns(head);
            _atomicInt.VolatileRead(new IntPtr(headIndex)).Returns(0);

            var counter = 0;
            MessageHandler h = (id, chunk) => counter++;
            var messagesRead = _ringBuffer.Read(h, 2);

            Assert.AreEqual(0, messagesRead);
            Assert.AreEqual(0, counter);

            _atomicLong.Received(1).VolatileRead(new IntPtr(headIndex));
            _atomicLong.DidNotReceiveWithAnyArgs().VolatileWrite(new IntPtr(), 0);
            _buffer.DidNotReceiveWithAnyArgs().ZeroMemory(0, 0);
        }

        [Test]
        public void ShouldReadTwoMessages()
        {
            const int msgLength = 16;
            var recordLength = HeaderLength + msgLength;
            var alignedRecordLength = recordLength.AlignToMultipleOf(RecordAlignment);
            long tail = alignedRecordLength * 2;
            const long head = 0L;
            const int headIndex = (int)head;

            _atomicLong.Read(Head).Returns(head);
            var makeHeader = MakeHeader(recordLength, MessageTypeId);
            _atomicLong.VolatileRead(new IntPtr(headIndex)).Returns(makeHeader);
            _atomicLong.VolatileRead(new IntPtr(headIndex + alignedRecordLength)).Returns(makeHeader);

            var counter = 0;
            MessageHandler h = (id, chunk) => counter++;
            var messagesRead = _ringBuffer.Read(h, 3);

            Assert.AreEqual(2, messagesRead);
            Assert.AreEqual(2, counter);

            Received.InOrder(() =>
            {
                _buffer.Received(1).ZeroMemory(headIndex, alignedRecordLength * 2);
                _atomicLong.VolatileWrite(Head, tail);
            });
        }

        [Test]
        public void ShouldLimitReadOfMessages()
        {
            const int msgLength = 16;
            var recordLength = HeaderLength + msgLength;
            var alignedRecordLength = recordLength.AlignToMultipleOf(RecordAlignment);
            const long head = 0L;
            const int headIndex = (int)head;

            _atomicLong.Read(Head).Returns(head);
            _atomicLong.VolatileRead(new IntPtr(headIndex)).Returns(MakeHeader(recordLength, MessageTypeId));

            var counter = 0;
            MessageHandler h = (id, chunk) => counter++;
            var messagesRead = _ringBuffer.Read(h, 1);

            Assert.AreEqual(1, messagesRead);
            Assert.AreEqual(1, counter);

            Received.InOrder(() =>
            {
                _buffer.ZeroMemory(headIndex, alignedRecordLength);
                _atomicLong.VolatileWrite(Head, head + alignedRecordLength);
            });
        }

        [Test]
        public void ShouldCopeWithExceptionFromHandler()
        {
            const int msgLength = 16;
            var recordLength = HeaderLength + msgLength;
            var alignedRecordLength = recordLength.AlignToMultipleOf(RecordAlignment);
            var tail = alignedRecordLength * 2;
            const long head = 0L;
            const int headIndex = (int)head;

            _atomicLong.Read(Head).Returns(head);
            _atomicLong.VolatileRead(new IntPtr(headIndex)).Returns(MakeHeader(recordLength, MessageTypeId));
            _atomicLong.VolatileRead(new IntPtr(headIndex + alignedRecordLength)).Returns(MakeHeader(recordLength, MessageTypeId));

            var counter = 0;
            MessageHandler h = (id, chunk) =>
            {
                counter++;
                if (counter == 2)
                {
                    throw new Exception();
                }
            };

            try
            {
                _ringBuffer.Read(h, 2);
            }
            catch
            {
                Assert.AreEqual(2, counter);

                Received.InOrder(() =>
                {
                    _buffer.Received(1).ZeroMemory(Arg.Is(headIndex), Arg.Is(alignedRecordLength * 2));
                    _atomicLong.Received(1).VolatileWrite(Head, tail);
                });
                return;
            }

            Assert.Fail("This should not go to this point");
        }

        [Test]
        public void ShouldThrowExceptionForCapacityThatIsNotPowerOfTwo()
        {
            var unsafeBuffer = Substitute.For<IUnsafeBuffer>();
            unsafeBuffer.Size.Returns(777 + TrailerLength);
            Assert.Throws<ArgumentException>(() => new ManyToOneRingBuffer(unsafeBuffer));
        }

        [Test]
        public void ShouldThrowExceptionWhenMaxMessageSizeExceeded()
        {
            Assert.Throws<ArgumentException>(() => { _ringBuffer.Write(MessageTypeId, new ByteChunk(null, _ringBuffer.MaximumMessageLength + 1)); });
        }

        [Test]
        public void ShouldInsertPaddingAndWriteToBuffer()
        {
            const int padding = 200;
            const int messageLength = 400;
            var recordLength = messageLength + HeaderLength;
            var alignedRecordLength = recordLength.AlignToMultipleOf(RecordAlignment);

            const long tail = 2 * Capacity - padding;
            const long head = tail;

            // free space is (200 + 300) more than message length (400) but contiguous space (300) is less than message length (400)
            const long headCache = Capacity + 300;

            _atomicLong.VolatileRead(Head).Returns(head);
            _atomicLong.VolatileRead(Tail).Returns(tail);
            _atomicLong.VolatileRead(HeadCounterCache).Returns(headCache);
            _atomicLong.CompareExchange(Tail, tail + alignedRecordLength + padding, tail).Returns(tail);

            byte* payload = stackalloc byte[messageLength];
            Assert.True(_ringBuffer.Write(MessageTypeId, new ByteChunk(payload, messageLength)));
        }
    }
}