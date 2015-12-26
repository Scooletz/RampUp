using System;

namespace RampUp.Buffers
{
    public unsafe interface ISegmentPool : IDisposable
    {
        bool TryPop(out Segment* result);
        int TryPop(int numberOfSegmentsToRetrieve, out Segment* startingSegment);
        void Push(Segment* segment);

        int SegmentLength { get; }
    }
}