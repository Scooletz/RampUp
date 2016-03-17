using System;

namespace RampUp.Buffers
{
    /// <summary>
    /// A pool of segments using FIFO strategy for managing them.
    /// </summary>
    public unsafe interface ISegmentPool : IDisposable
    {
        bool TryPop(out Segment* result);
        int TryPop(int numberOfSegmentsToRetrieve, out Segment* startingSegment);

        /// <summary>
        /// Returns the segment to the pool.
        /// </summary>
        void Push(Segment* segment);

        /// <summary>
        /// Segment size, a power of 2, not lower than 4096
        /// </summary>
        int SegmentLength { get; }
    }
}