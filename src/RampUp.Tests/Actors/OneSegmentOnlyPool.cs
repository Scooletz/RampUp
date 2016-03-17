using System;
using System.Runtime.InteropServices;
using RampUp.Buffers;

namespace RampUp.Tests.Actors
{
    public unsafe class OneSegmentOnlyPool : ISegmentPool
    {
        private readonly byte[] _managedBytes = new byte[SingleThreadSegmentPool.SegmentSize + sizeof (Segment)];
        private readonly GCHandle _handle;
        private Segment* _segment;

        public OneSegmentOnlyPool()
        {
            _handle = GCHandle.Alloc(_managedBytes, GCHandleType.Pinned);
            var bytes = (byte*) _handle.AddrOfPinnedObject();
            _segment = (Segment*) (bytes + SingleThreadSegmentPool.SegmentSize);
            *_segment = new Segment(bytes, SingleThreadSegmentPool.SegmentSize);
            SegmentLength = SingleThreadSegmentPool.SegmentSize;
        }

        public bool HasSegment => _segment != null;

        public void Dispose()
        {
            _handle.Free();
        }

        public bool TryPop(out Segment* result)
        {
            result = _segment;
            _segment = null;
            return result != null;
        }

        public int TryPop(int numberOfSegmentsToRetrieve, out Segment* startingSegment)
        {
            throw new System.NotImplementedException();
        }

        public void Push(Segment* segment)
        {
            if (_segment != null)
            {
                throw new InvalidOperationException();
            }

            _segment = segment;
        }

        public int SegmentLength { get; }
    }
}