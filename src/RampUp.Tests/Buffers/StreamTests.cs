using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RampUp.Buffers;

namespace RampUp.Tests.Buffers
{
    public class StreamTests : IDisposable
    {
        private readonly ISegmentPool _pool;
        private Stream _stream;

        public StreamTests()
        {
            _pool = new SingleThreadSegmentPool(1024, 4096);
        }

        [SetUp]
        public void SetUp()
        {
            _stream = _pool.GetStream();
        }

        [TearDown]
        public void TearDown()
        {
            _stream.Dispose();
        }

        [Test]
        public void WhenSeekingBeyond_ExceptionIsThrown()
        {
            _stream.WriteByte(1);
            _stream.WriteByte(2);
            Throws(() => { _stream.Seek(3, SeekOrigin.Begin); });
        }

        [Test]
        public void WhenSeekingFromBeginning_ThenMovingFromBeginning()
        {
            const int c = 100;
            _stream.Write(new byte[c], 0, c);

            const int p = 23;
            _stream.Seek(p, SeekOrigin.Begin);
            Assert.AreEqual(p, _stream.Position);
        }

        [Test]
        public void WhenSeekingFromEnd_ThenMovingFromEnd()
        {
            const int c = 100;
            _stream.Write(new byte[c], 0, c);

            const int p = -23;
            _stream.Seek(p, SeekOrigin.End);
            Assert.AreEqual(c + p, _stream.Position);
        }

        [Test]
        public void WhenSeekingFromCurrent_ThenMovingByTheChange()
        {
            const int c = 100;
            _stream.Write(new byte[c], 0, c);
            _stream.Seek(0, SeekOrigin.Begin);
            _stream.ReadByte();

            var position = _stream.Position;
            const int offset = 10;
            _stream.Seek(offset, SeekOrigin.Current);
            Assert.AreEqual(offset, _stream.Position - position);
        }

        [Test]
        public void WhenWriting_PositionIsIncremented()
        {
            var bytes = new byte[1023];
            var position = 0;
            for (var i = 0; i < 10; i++)
            {
                _stream.Write(bytes, 0, bytes.Length);
                position += bytes.Length;

                Assert.AreEqual(position, _stream.Position);
            }
        }

        [Test]
        public void WhenWritingByByte_PositionIsIncremented()
        {
            var position = 0;
            for (var i = 0; i < 5000; i++)
            {
                _stream.WriteByte(1);
                position += 1;

                Assert.AreEqual(position, _stream.Position);
            }
        }

        private static void Throws(TestDelegate testDelegate)
        {
            Assert.Throws(Is.AssignableTo(typeof (Exception)), testDelegate);
        }

        [Test]
        public void WhenReading_PositionIsIncremented()
        {
            _stream.Write(new byte[100], 0, 100);
            _stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(0, _stream.Position);
            const int count = 10;
            _stream.Read(new byte[count], 0, count);
            Assert.AreEqual(count, _stream.Position);
        }

        [Test]
        public void WhenReadingByByte_PositionIsIncremented()
        {
            _stream.Write(new byte[100], 0, 100);
            _stream.Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(0, _stream.Position);
            _stream.ReadByte();
            Assert.AreEqual(1, _stream.Position);
        }

        [Test]
        public void WhenReadingBeyondStream_NothingRead()
        {
            const int c = 100;
            _stream.Write(new byte[c], 0, c);
            _stream.Seek(0, SeekOrigin.Begin);
            _stream.Read(new byte[c], 0, c);

            var buffer = new byte[2];
            var read = _stream.Read(buffer, 0, 2);
            Assert.AreEqual(0, read);
            CollectionAssert.AreEquivalent(new byte[] {0, 0}, buffer);
        }

        [Test]
        public void WhenReadingBeyondStreamByByte_NothingRead()
        {
            const int c = 100;
            _stream.Write(new byte[c], 0, c);
            _stream.Seek(0, SeekOrigin.Begin);
            _stream.Read(new byte[c], 0, c);

            var read = _stream.ReadByte();
            Assert.AreEqual(-1, read);
        }


        [Test]
        public void WhenSeekFromBeginningPositionIsSetFromBeginning()
        {
            const int length = 100;
            const int seekBy = 22;
            _stream.Write(new byte[length], 0, length);
            _stream.Seek(seekBy, SeekOrigin.Begin);
            Assert.AreEqual(seekBy, _stream.Position);
        }

        [Test]
        public void WhenSeekFromEndPositionIsSetFromEnd()
        {
            const int length = 100;
            const int seekBy = -22;
            _stream.Write(new byte[length], 0, length);
            _stream.Seek(seekBy, SeekOrigin.End);
            Assert.AreEqual(length + seekBy, _stream.Position);
        }

        [TestCase(-1L, ExpectedException = typeof (ArgumentException))]
        [TestCase(0L)]
        [TestCase(1L)]
        [TestCase(1L)]
        [TestCase(8012L)]
        public void SetLength(long length)
        {
            _stream.SetLength(length);

            Assert.AreEqual(length, _stream.Length);
        }

        [TestCase(new object[] {new[] {10, 200, 400, 800, 1000, 2000, 4000}}, TestName = "Short to long elements")]
        [TestCase(new object[] {new[] {4094, 1, 2, 4090, 127}}, TestName = "Buffer boundaries")]
        public void ReadWrite(int[] lengths)
        {
            var total = lengths.Sum();
            var random = new Random(total);

            var bytes = new byte[total];
            random.NextBytes(bytes);

            var offset = 0;
            foreach (var length in lengths)
            {
                _stream.Write(bytes, offset, length);
                offset += length;
            }

            _stream.Seek(0, SeekOrigin.Begin);

            var ms = new MemoryStream(total);
            _stream.CopyTo(ms, 13);

            CollectionAssert.AreEqual(bytes, ms.ToArray());
        }

        [TestCase(0, TestName = "No offset at all")]
        [TestCase(1, TestName = "Skipping one")]
        public void NewCopyShouldCopyLikeBaseCopy(int offset)
        {
            var bytes = new byte[9345];
            var random = new Random(bytes.Length);
            random.NextBytes(bytes);

            var original = new MemoryStream(bytes);
            original.Seek(0, SeekOrigin.Begin);
            original.CopyTo(_stream);
            _stream.Seek(offset, SeekOrigin.Begin);

            var newCopy = new MemoryStream();
            ((SegmentStream) _stream).CopyTo(newCopy);

            CollectionAssert.AreEqual(bytes.Skip(offset), newCopy.ToArray());
        }

        public void Dispose()
        {
            _pool.Dispose();
        }
    }
}