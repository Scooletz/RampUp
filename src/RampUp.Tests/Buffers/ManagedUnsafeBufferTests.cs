using System;
using System.ComponentModel;
using NUnit.Framework;
using RampUp.Buffers;

namespace RampUp.Tests.Buffers
{
    public unsafe class ManagedUnsafeBufferTests
    {
        private ManagedUnsafeBuffer _buffer;
        private const int Size = 128;

        [SetUp]
        public void SetUp()
        {
            _buffer = new ManagedUnsafeBuffer(Size);
        }

        [TearDown]
        public void TearDown()
        {
            _buffer.Dispose();
            _buffer = null;
        }

        [Test]
        public void WhenRangeInBoundsRequested_ThenItsTranslatedProperly()
        {
            const int value = 345493409;
            _buffer.GetAtomicInt(0).Write(value);

            var chunk = new ByteChunk(_buffer.RawBytes, 4);
            var bytes = _buffer.Translate(chunk);

            var read = BitConverter.ToInt32(bytes.Array, bytes.Offset);

            Assert.AreEqual(value, read);
        }

        [Test]
        public void WhenRangeBeforeBufferRequested_ThenThrows()
        {
            var chunk = new ByteChunk(_buffer.RawBytes - 1, 4);
            Assert.Throws<ArgumentException>(() => _buffer.Translate(chunk));
        }

        [Test]
        public void WhenRangeAfterBufferRequested_ThenThrows()
        {
            var chunk = new ByteChunk(_buffer.RawBytes, Size + 1);
            Assert.Throws<ArgumentException>(() => _buffer.Translate(chunk));
        }
    }
}