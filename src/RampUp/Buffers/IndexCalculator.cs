using System;
using System.Diagnostics.Contracts;

namespace RampUp.Buffers
{
    /// <summary>
    /// The calculator for index of the segment and the offset within segment for the given index.
    /// </summary>
    public struct IndexCalculator
    {
        private readonly int _indexInSegmentMask;
        private readonly int _segmentNumberShift;

        public IndexCalculator(int segmentLength)
        {
            if (segmentLength <= 1)
            {
                throw new ArgumentException("The segmentLength must be positive and greater than 1", nameof(segmentLength));
            }
            if (segmentLength.IsPowerOfTwo() == false)
            {
                throw new ArgumentException("Segment size must be power of 2", nameof(segmentLength));
            }

            _indexInSegmentMask = segmentLength - 1;
            _segmentNumberShift = segmentLength.Log2();
        }

        [Pure]
        public int GetSegmentIndex(long index)
        {
            return (int) (index >> _segmentNumberShift);
        }

        [Pure]
        public int GetIndexInSegment(long index)
        {
            return (int) (index & _indexInSegmentMask);
        }
    }
}