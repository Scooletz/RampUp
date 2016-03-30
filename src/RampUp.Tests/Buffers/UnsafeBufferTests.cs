using System;
using NUnit.Framework;
using RampUp.Buffers;

namespace RampUp.Tests.Buffers
{
    public class UnsafeBufferTests
    {
        [Test]
        public void WhenAllocatedAndDisposed_ShouldFreeMemory()
        {
            using (var buffer = new UnsafeBuffer(short.MaxValue + 1))
            {
                GC.KeepAlive(buffer);
            }
        }
    }
}