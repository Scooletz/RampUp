namespace RampUp.Buffers
{
    public static class Extensions
    {
        public static SegmentStream GetStream(this ISegmentPool pool)
        {
            return new SegmentStream(pool);
        }
    }
}