using System;

namespace RampUp.Buffers
{
    public static class Extensions
    {
        public static SegmentStream GetStream(this ISegmentPool pool)
        {
            return new SegmentStream(pool);
        }

        public static unsafe Segment* Pop(this ISegmentPool pool)
        {
            Segment* result;
            if (pool.TryPop(out result) == false)
            {
                throw new InvalidOperationException("Pool cannot provide more memory");
            }

            return result;
        }
    }
}