using System;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using RampUp.Atomics;
using RampUp.Buffers;
using RampUp.Ring;
using static RampUp.Ring.RingBufferDescriptor;
namespace RampUp.Tests.Ring
{
    public class ManyToOneBufferTests
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
        private static readonly IntPtr Tail = new IntPtr(8);
        private static readonly IntPtr Head = new IntPtr(16);
        private static readonly IntPtr HeadCache = new IntPtr(24);

        [SetUp]
        public void SetUp()
        {
            _buffer = Substitute.For<IUnsafeBuffer>();
            _buffer.Size.Returns(TotalBufferLength);
        }

        [Test]
        public unsafe void ShouldWriteToEmptyBuffer()
        {
            const int length = 8;
            var recordLength = length + HeaderLength;
            var alignedRecordLength = recordLength.AlignToMultipleOf(Native.CacheLineSize);

            var headAtomic = new AtomicLong((byte*)Head);
            _buffer.GetAtomicLong(HeadCounterIndex).Returns(headAtomic);
            var tailAtomic = new AtomicLong((byte*)Tail);
            _buffer.GetAtomicLong(TailCounterIndex).Returns(tailAtomic);
            var headCacheAtomic = new AtomicLong((byte*)HeadCache);
            _buffer.GetAtomicLong(HeadCounterCacheIndex).Returns(headCacheAtomic);

            var atomicLong = Substitute.For<Mocks.IAtomicLong>();
            Mocks.AtomicLong = atomicLong;

            var atomicInt = Substitute.For<Mocks.IAtomicInt>();
            Mocks.AtomicInt = atomicInt;

            const int headValue = 0;
            atomicLong.VolatileRead(Arg.Is(Head)).Returns(headValue);
            const int tailValue = 0;
            atomicLong.VolatileRead(Arg.Is(Tail)).Returns(tailValue);

            _ringBuffer = new ManyToOneRingBuffer(_buffer);

            atomicLong.CompareExchange(Arg.Is(Tail), Arg.Is(tailValue), Arg.Is(tailValue + alignedRecordLength)).Returns(tailValue);

            var block = stackalloc byte[100];

            _buffer.GetAtomicInt(tailValue).Returns(new AtomicInt((byte*)CurrentSlot));
            var chunk = new ByteChunk(block, length);
            Assert.IsTrue(_ringBuffer.Write(MessageTypeId, chunk));

            Received.InOrder(() =>
            {
                atomicLong.VolatileWrite(CurrentSlot, MakeHeader(-recordLength, MessageTypeId));
                _buffer.Write(EncodedMsgOffset(tailValue), chunk);
                _buffer.GetAtomicInt(tailValue);
                atomicInt.VolatileWrite(CurrentSlot, recordLength);
            });
        }
    }
}