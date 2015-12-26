using System;
using NUnit.Framework;
using RampUp.Buffers;

namespace RampUp.Tests.Buffers
{
    public class IndexCalculatorTests
    {
        [TestCase(0, 0, 0, 0, ExpectedException = typeof(ArgumentException))]
        [TestCase(1, 0, 0, 0, ExpectedException = typeof(ArgumentException))]
        [TestCase(3, 0, 0, 0, ExpectedException = typeof(ArgumentException))]
        [TestCase(1023, 0, 0, 0, ExpectedException = typeof(ArgumentException))]

        [TestCase(1024, 0, 0, 0)]
        [TestCase(1024, 1, 0, 1)]
        [TestCase(1024, 1024, 1, 0)]
        [TestCase(1024, 1025, 1, 1)]
        public void Test(int segmentLength, long index, int expectedSegmentIndex, int expectedIndexInSegment)
        {
            var calculator = new IndexCalculator(segmentLength);
            Assert.AreEqual(expectedIndexInSegment, calculator.GetIndexInSegment(index));
            Assert.AreEqual(expectedSegmentIndex, calculator.GetSegmentIndex(index));
        }
    }
}