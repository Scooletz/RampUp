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

            _buffer.GetAtomicLong(Arg.Any<long>()).Returns(ci => new AtomicLong((byte*) ci.Arg<long>()));
            _buffer.GetAtomicInt(Arg.Any<long>()).Returns(ci => new AtomicInt((byte*) ci.Arg<long>()));

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

        //    @Test
        //public void shouldLimitReadOfMessages()
        //    {
        //        final int msgLength = 16;
        //        final int recordLength = HEADER_LENGTH + msgLength;
        //        final int alignedRecordLength = align(recordLength, ALIGNMENT);
        //        final long head = 0L;
        //        final int headIndex = (int)head;

        //        when(buffer.getLong(HEAD_COUNTER_INDEX)).thenReturn(head);
        //        when(buffer.getLongVolatile(headIndex)).thenReturn(makeHeader(recordLength, MSG_TYPE_ID));

        //        final int[] times = new int[1];
        //        final MessageHandler handler = (msgTypeId, buffer, index, length) ->times[0]++;
        //        final int limit = 1;
        //        final int messagesRead = ringBuffer.read(handler, limit);

        //        assertThat(messagesRead, is(1));
        //        assertThat(times[0], is(1));

        //        final InOrder inOrder = inOrder(buffer);
        //        inOrder.verify(buffer, times(1)).setMemory(headIndex, alignedRecordLength, (byte)0);
        //        inOrder.verify(buffer, times(1)).putLongOrdered(HEAD_COUNTER_INDEX, head + alignedRecordLength);
        //    }

        //    @Test
        //public void shouldCopeWithExceptionFromHandler()
        //    {
        //        final int msgLength = 16;
        //        final int recordLength = HEADER_LENGTH + msgLength;
        //        final int alignedRecordLength = align(recordLength, ALIGNMENT);
        //        final long tail = alignedRecordLength * 2;
        //        final long head = 0L;
        //        final int headIndex = (int)head;

        //        when(buffer.getLong(HEAD_COUNTER_INDEX)).thenReturn(head);
        //        when(buffer.getLongVolatile(headIndex)).thenReturn(makeHeader(recordLength, MSG_TYPE_ID));
        //        when(buffer.getLongVolatile(headIndex + alignedRecordLength)).thenReturn(makeHeader(recordLength, MSG_TYPE_ID));

        //        final int[] times = new int[1];
        //        final MessageHandler handler =
        //            (msgTypeId, buffer, index, length) ->
        //            {
        //            times[0]++;
        //            if (times[0] == 2)
        //            {
        //                throw new RuntimeException();
        //            }
        //        };

        //        try
        //        {
        //            ringBuffer.read(handler);
        //        }
        //        catch (final RuntimeException ignore)
        //    {
        //            assertThat(times[0], is(2));

        //            final InOrder inOrder = inOrder(buffer);
        //            inOrder.verify(buffer, times(1)).setMemory(headIndex, alignedRecordLength * 2, (byte)0);
        //            inOrder.verify(buffer, times(1)).putLongOrdered(HEAD_COUNTER_INDEX, tail);

        //            return;
        //        }

        //        fail("Should have thrown exception");
        //        }

        //        @Test
        //public void shouldNotUnblockWhenEmpty()
        //    {
        //        final long position = ALIGNMENT * 4;
        //        when(buffer.getLongVolatile(HEAD_COUNTER_INDEX)).thenReturn(position);
        //        when(buffer.getLongVolatile(TAIL_COUNTER_INDEX)).thenReturn(position);

        //        assertFalse(ringBuffer.unblock());
        //    }

        //    @Test
        //public void shouldUnblockMessageWithHeader()
        //    {
        //        final int messageLength = ALIGNMENT * 4;
        //        when(buffer.getLongVolatile(HEAD_COUNTER_INDEX)).thenReturn((long)messageLength);
        //        when(buffer.getLongVolatile(TAIL_COUNTER_INDEX)).thenReturn((long)messageLength * 2);
        //        when(buffer.getIntVolatile(messageLength)).thenReturn(-messageLength);

        //        assertTrue(ringBuffer.unblock());

        //        verify(buffer).putLongOrdered(messageLength, makeHeader(messageLength, PADDING_MSG_TYPE_ID));
        //    }

        //    @Test
        //public void shouldUnblockGapWithZeros()
        //    {
        //        final int messageLength = ALIGNMENT * 4;
        //        when(buffer.getLongVolatile(HEAD_COUNTER_INDEX)).thenReturn((long)messageLength);
        //        when(buffer.getLongVolatile(TAIL_COUNTER_INDEX)).thenReturn((long)messageLength * 3);
        //        when(buffer.getIntVolatile(messageLength * 2)).thenReturn(messageLength);

        //        assertTrue(ringBuffer.unblock());

        //        verify(buffer).putLongOrdered(messageLength, makeHeader(messageLength, PADDING_MSG_TYPE_ID));
        //    }

        //    @Test
        //public void shouldNotUnblockGapWithMessageRaceOnSecondMessageIncreasingTailThenInterrupting()
        //    {
        //        final int messageLength = ALIGNMENT * 4;
        //        when(buffer.getLongVolatile(HEAD_COUNTER_INDEX)).thenReturn((long)messageLength);
        //        when(buffer.getLongVolatile(TAIL_COUNTER_INDEX)).thenReturn((long)messageLength * 3);
        //        when(buffer.getIntVolatile(messageLength * 2)).thenReturn(0).thenReturn(messageLength);

        //        assertFalse(ringBuffer.unblock());
        //        verify(buffer, never()).putLongOrdered(messageLength, makeHeader(messageLength, PADDING_MSG_TYPE_ID));
        //    }

        //    @Test
        //public void shouldNotUnblockGapWithMessageRaceWhenScanForwardTakesAnInterrupt()
        //    {
        //        final int messageLength = ALIGNMENT * 4;
        //        when(buffer.getLongVolatile(HEAD_COUNTER_INDEX)).thenReturn((long)messageLength);
        //        when(buffer.getLongVolatile(TAIL_COUNTER_INDEX)).thenReturn((long)messageLength * 3);
        //        when(buffer.getIntVolatile(messageLength * 2)).thenReturn(0).thenReturn(messageLength);
        //        when(buffer.getIntVolatile(messageLength * 2 + ALIGNMENT)).thenReturn(7);

        //        assertFalse(ringBuffer.unblock());
        //        verify(buffer, never()).putLongOrdered(messageLength, makeHeader(messageLength, PADDING_MSG_TYPE_ID));
        //    }

        //    @Test
        //public void shouldCalculateCapacityForBuffer()
        //    {
        //        assertThat(ringBuffer.capacity(), is(CAPACITY));
        //    }

        //@Test(expected = IllegalStateException.class)
        //public void shouldThrowExceptionForCapacityThatIsNotPowerOfTwo()
        //    {
        //        final int capacity = 777;
        //        final int totalBufferLength = capacity + RingBufferDescriptor.TRAILER_LENGTH;
        //        new ManyToOneRingBuffer(new UnsafeBuffer(new byte[totalBufferLength]));
        //    }

        //@Test(expected = IllegalArgumentException.class)
        //public void shouldThrowExceptionWhenMaxMessageSizeExceeded()
        //    {
        //        final UnsafeBuffer srcBuffer = new UnsafeBuffer(new byte[1024]);

        //        ringBuffer.write(MSG_TYPE_ID, srcBuffer, 0, ringBuffer.maxMsgLength() + 1);
        //    }

        //    @Test
        //public void shouldInsertPaddingAndWriteToBuffer()
        //    {
        //        final int padding = 200;
        //        final int messageLength = 400;
        //        final int recordLength = messageLength + HEADER_LENGTH;
        //        final int alignedRecordLength = align(recordLength, ALIGNMENT);

        //        final long tail = 2 * CAPACITY - padding;
        //        final long head = tail;

        //        // free space is (200 + 300) more than message length (400) but contiguous space (300) is less than message length (400)
        //        final long headCache = CAPACITY + 300;

        //        when(buffer.getLongVolatile(HEAD_COUNTER_INDEX)).thenReturn(head);
        //        when(buffer.getLongVolatile(TAIL_COUNTER_INDEX)).thenReturn(tail);
        //        when(buffer.getLongVolatile(HEAD_COUNTER_CACHE_INDEX)).thenReturn(headCache);
        //        when(buffer.compareAndSetLong(TAIL_COUNTER_INDEX, tail, tail + alignedRecordLength + padding)).thenReturn(true);
        //        final UnsafeBuffer srcBuffer = new UnsafeBuffer(new byte[messageLength]);
        //        assertTrue(ringBuffer.write(MSG_TYPE_ID, srcBuffer, 0, messageLength));
        //    }
    }
}