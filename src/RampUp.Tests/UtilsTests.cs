using NUnit.Framework;

namespace RampUp.Tests
{
    public class UtilsTests
    {
        [TestCase(1, Result = true)]
        [TestCase(2, Result = true)]
        [TestCase(3, Result = false)]
        [TestCase(4, Result = true)]
        [TestCase(8, Result = true)]
        [TestCase(13, Result = false)]
        [TestCase(24, Result = false)]
        public bool IsPowerOf2(int value)
        {
            return value.IsPowerOfTwo();
        }

        [TestCase(1L, Result = true)]
        [TestCase(2L, Result = true)]
        [TestCase(3L, Result = false)]
        [TestCase(4L, Result = true)]
        [TestCase(8L, Result = true)]
        [TestCase(13L, Result = false)]
        [TestCase(24L, Result = false)]
        public bool IsPowerOf2(long value)
        {
            return value.IsPowerOfTwo();
        }

        [TestCase(1, Result = 0)]
        [TestCase(2, Result = 1)]
        [TestCase(3, Result = 1)]
        [TestCase(4, Result = 2)]
        [TestCase(5, Result = 2)]
        [TestCase(8, Result = 3)]
        [TestCase(13, Result = 3)]
        [TestCase(24, Result = 4)]
        public int Log2(int value)
        {
            return value.Log2();
        }

        [TestCase(1L, Result = 0)]
        [TestCase(2L, Result = 1)]
        [TestCase(3L, Result = 1)]
        [TestCase(4L, Result = 2)]
        [TestCase(5L, Result = 2)]
        [TestCase(8L, Result = 3)]
        [TestCase(13L, Result = 3)]
        [TestCase(24L, Result = 4)]
        public int Log2(long value)
        {
            return value.Log2();
        }

        [TestCase(0, 64, Result = 0)]
        [TestCase(1, 64, Result = 64)]
        [TestCase(2, 64, Result = 64)]
        [TestCase(63, 64, Result = 64)]
        [TestCase(64, 64, Result = 64)]
        [TestCase(65, 64, Result = 128)]
        public int Align(int value, int alignment)
        {
            return value.AlignToMultipleOf(alignment);
        }
    }
}