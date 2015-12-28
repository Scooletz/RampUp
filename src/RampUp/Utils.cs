using System.Runtime.CompilerServices;

namespace RampUp
{
    public static class Util
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(this int n)
        {
            return (n & (n - 1)) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(this long n)
        {
            return (n & (n - 1)) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Log2(this int i)
        {
            var r = 0;
            while ((i >>= 1) != 0)
            {
                ++r;
            }
            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Log2(this long i)
        {
            var r = 0;
            while ((i >>= 1) != 0)
            {
                ++r;
            }
            return r;
        }

        ///<summary>
        /// Aligns <paramref name="value"/> to the next multiple of <paramref name="alignment"/>.
        /// If the value equals an alignment multiple then it's returned without changes.
        /// </summary>
        /// <remarks>
        /// No branching :D
        /// </remarks>
        public static int AlignToMultipleOf(this int value, int alignment)
        {
            return (value + (alignment - 1)) & ~(alignment - 1);
        }
    }
}