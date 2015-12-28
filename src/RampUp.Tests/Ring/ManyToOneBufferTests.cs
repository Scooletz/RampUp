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
        private const int MsgTypeId = 7;
        private const int Capacity = 4096;
        private static readonly int TotalBufferLength = Capacity + TrailerLength;
        private static readonly int TailCounterIndex = Capacity + TailPositionOffset;
        private static readonly int HeadCounterIndex = Capacity + HeadPositionOffset;
        private static readonly int HeadCounterCacheIndex = Capacity + HeadCachePositionOffset;

        private IUnsafeBuffer _buffer;
        private ManyToOneRingBuffer _ringBuffer;

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
            var tail = 0L;
            var head = 0L;
            var headCache = 0L;

            var headAtomic = new AtomicLong((byte*) &head);
            _buffer.GetAtomicLong(HeadCounterIndex).Returns(headAtomic);
            var tailAtomic = new AtomicLong((byte*) &tail);
            _buffer.GetAtomicLong(TailCounterIndex).Returns(tailAtomic);
            var headCacheAtomic = new AtomicLong((byte*) &headCache);
            _buffer.GetAtomicLong(HeadCounterCacheIndex).Returns(headCacheAtomic);

            _ringBuffer = new ManyToOneRingBuffer(_buffer);
            //when(buffer.getLongVolatile(HEAD_COUNTER_INDEX)).thenReturn(head);
            //when(buffer.getLongVolatile(TAIL_COUNTER_INDEX)).thenReturn(tail);


            // TODO: when(buffer.compareAndSetLong(TAIL_COUNTER_INDEX, tail, tail + alignedRecordLength)).thenReturn(TRUE);

            var block = stackalloc byte[100];

            Assert.IsTrue(_ringBuffer.Write(MsgTypeId, new ByteChunk(block,length)));
      
            // TODO: asserts on Atomics
            //final InOrder inOrder = inOrder(buffer);
            //inOrder.verify(buffer).putLongOrdered((int)tail, makeHeader(-recordLength, MSG_TYPE_ID));
            //inOrder.verify(buffer).putBytes(encodedMsgOffset((int)tail), srcBuffer, srcIndex, length);
            //inOrder.verify(buffer).putIntOrdered(lengthOffset((int)tail), recordLength);
        }
    }
}