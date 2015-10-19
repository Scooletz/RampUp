using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RampUp.Buffers;

namespace RampUp.Tests.Buffers
{
    public class StreamTests
    {
        private readonly Segment.Pool _pool;
        private Stream _stream;

        public StreamTests()
        {
            _pool = new Segment.Pool(32);
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
            _stream.Write(new byte[c],0, c);
            
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
            Assert.Throws(Is.AssignableTo(typeof(Exception)), testDelegate);
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

        //[TestFixture]
        //public class when_seeking_in_the_stream : has_buffer_pool_fixture
        //{
        //    [Test]
        //    public void from_begin_sets_relative_to_beginning()
        //    {
        //        BufferPoolStream stream = new BufferPoolStream(BufferPool);
        //        stream.Write(new byte[500], 0, 500);
        //        stream.Seek(22, SeekOrigin.Begin);
        //        Assert.AreEqual(22, stream.Position);
        //    }

        //    [Test]
        //    public void from_end_sets_relative_to_end()
        //    {
        //        BufferPoolStream stream = new BufferPoolStream(BufferPool);
        //        stream.Write(new byte[500], 0, 500);
        //        stream.Seek(-100, SeekOrigin.End);
        //        Assert.AreEqual(400, stream.Position);
        //    }

        //    [Test]
        //    public void from_current_sets_relative_to_current()
        //    {
        //        BufferPoolStream stream = new BufferPoolStream(BufferPool);
        //        stream.Write(new byte[500], 0, 500);
        //        stream.Seek(-2, SeekOrigin.Current);
        //        stream.Seek(1, SeekOrigin.Current);
        //        Assert.AreEqual(499, stream.Position);
        //    }

        //    [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        //    public void a_negative_position_throws_an_argumentexception()
        //    {
        //        BufferPoolStream stream = new BufferPoolStream(BufferPool);
        //        stream.Seek(-1, SeekOrigin.Begin);
        //    }

        //    [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        //    public void seeking_past_end_of_stream_throws_an_argumentexception()
        //    {
        //        BufferPoolStream stream = new BufferPoolStream(BufferPool);
        //        stream.Write(new byte[500], 0, 500);
        //        stream.Seek(501, SeekOrigin.Begin);
        //    }
        //}

    }
}