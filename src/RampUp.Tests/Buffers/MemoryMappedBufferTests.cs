using System;
using NUnit.Framework;
using RampUp.Buffers;

namespace RampUp.Tests.Buffers
{
    public class MemoryMappedBufferTests
    {
        [Test]
        public void When_created_and_opened_Should_be_visible()
        {
            var name = "Global\\" + Guid.NewGuid();
            const int count = 256;
            const int size = count * sizeof(int);

            using (var created = MemoryMappedUnsafeBuffer.Create(size, name))
            {
                for (var i = 0; i < count; i++)
                {
                    created.GetAtomicInt(GetOffset(i)).Write(i);
                }

                using (var opened = MemoryMappedUnsafeBuffer.OpenExisting(name))
                {
                    for (var i = 0; i < size; i++)
                    {
                        Assert.AreEqual(i, opened.GetAtomicInt(GetOffset(i)).Read());
                    }
                }
            }
        }

        private static int GetOffset(int index)
        {
            return index * sizeof(int);
        }
    }
}