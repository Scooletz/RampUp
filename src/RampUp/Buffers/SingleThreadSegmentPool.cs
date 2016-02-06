using System;

namespace RampUp.Buffers
{
    public sealed unsafe class SingleThreadSegmentPool : ISegmentPool
    {
        public const int MinimalSegmentCount = 1024;
        public const int SegmentSize = 4096;
        private readonly UnsafeBuffer _buffer;
        private Segment* _head;

        public SingleThreadSegmentPool(int segmentCount)
        {
            if (segmentCount < 1024)
            {
                throw new ArgumentException($"SegmentCount must be at least {MinimalSegmentCount}", nameof(segmentCount));
            }

            var segmentStructureOverhead = segmentCount * Segment.Size;
            var segmentData = segmentCount * SegmentSize;

            _buffer = new UnsafeBuffer(segmentData + segmentStructureOverhead);

            var segments = (Segment*)_buffer.RawBytes;
            var data = _buffer.RawBytes + segmentStructureOverhead;

            for (var i = 0; i < segmentCount; i++)
            {
                var s = segments + i;
                var buffer = data + i * SegmentSize;
                var segment = new Segment(buffer, SegmentSize);

                // copy to the memory pointed by s
                Native.MemcpyUnmanaged((byte*) s, (byte*) &segment, Segment.Size);

                Push(s);
            }
        }

        public int SegmentLength => SegmentSize;

        public bool TryPop(out Segment* result)
        {
            if (_head == null)
            {
                result = null;
                return false;
            }

            result = _head;
            _head = _head->Next;
            result->Next = null;

            return true;
        }

        public int TryPop(int numberOfSegmentsToRetrieve, out Segment* startingSegment)
        {
            if (_head == null)
            {
                startingSegment = null;
                return 0;
            }

            var next = _head;
            var nodesCount = 1;
            for (; nodesCount < numberOfSegmentsToRetrieve && next->Next != null; nodesCount++)
            {
                next = next->Next;
            }

            startingSegment = _head;
            _head = next->Next;
            next->Next = null; // cut the end
            return nodesCount;
        }

        public void Push(Segment* segment)
        {
            if (segment->Length != SegmentLength)
            {
                throw new ArgumentException("The segment length is different from the segment sizes of this pool. Are you trying to push a segment from another pool maybe?");
            }

            var tail = segment->Tail;
            if (tail == null)
            {
                segment->Next = _head;
            }
            else
            {
                tail->Next = _head;
            }
            _head = segment; 
        }

        public void Dispose()
        {
            _buffer.Dispose();
        }
    }
}