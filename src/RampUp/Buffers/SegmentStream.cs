using System;
using System.IO;

namespace RampUp.Buffers
{
    public sealed unsafe class SegmentStream : Stream
    {
        private readonly ISegmentPool _pool;
        private readonly IndexCalculator _calculator;
        private Segment* _head = null;
        private Segment* _tail = null;
        private long _length;
        private long _position;
        private long _capacity;

        internal SegmentStream(ISegmentPool pool)
        {
            _pool = pool;
            _calculator = new IndexCalculator(pool.SegmentLength);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.End:
                    Position = _length + offset;
                    break;
                case SeekOrigin.Current:
                    Position = _position + offset;
                    break;
                default:
                    throw new ArgumentException("SeekOrigin: " + origin);
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            if (value < 0)
            {
                throw new ArgumentException("Length must be greater than 0", "value");
            }

            if (value == 0)
            {
                if (_head != null)
                {
                    _pool.Push(_head);
                }

                _head = null;
                _tail = null;
                _length = 0;
                _position = 0;
                _capacity = 0;
                return;
            }

            var currentNumberOfSegments = _calculator.GetSegmentIndex(_length);
            var nextNumberOfSegments = _calculator.GetSegmentIndex(value);

            if (currentNumberOfSegments < nextNumberOfSegments)
            {
                // enlarging the stream is fast with _tail remembered
                var segmentsToAdd = nextNumberOfSegments - currentNumberOfSegments;
                AddSegments(segmentsToAdd);
            }
            else if (currentNumberOfSegments > nextNumberOfSegments)
            {
                throw new NotImplementedException("Not implemented yet");
            }

            _length = value;
            if (_position > value)
            {
                _position = value;
            }
        }

        /// <summary>
        /// Adds segments, returning the initial segment of the newly added chain.
        /// </summary>
        /// <param name="segmentsToObtain"></param>
        /// <returns>Returns the initial segment of the newly added chain.</returns>
        private Segment* AddSegments(int segmentsToObtain)
        {
            Segment* segment;
            if (_pool.TryPop(segmentsToObtain, out segment) == segmentsToObtain)
            {
                if (_head == null)
                {
                    _head = segment;
                    _tail = GetTailOrThis(segment);
                }
                else
                {
                    // attach the new segment to the end O(1)
                    _tail->Next = segment;
                    _tail = GetTailOrThis(segment); // rewrite tail O(number of segments added)
                }

                var s = segment;
                while (s != null)
                {
                    _capacity += s->Length;
                    s = s->Next;
                }

                return segment;
            }
            else
            {
                // not enough elements taken, release and throw
                _pool.Push(segment);
                throw new Exception("Not enough memory in the pool");
            }
        }

        private static Segment* GetTailOrThis(Segment* segment)
        {
            if (segment == null)
            {
                return null;
            }

            var tail = segment;
            while (tail->Next != null)
            {
                tail = tail->Next;
            }
            return tail;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _length)
                return 0;

            var alreadyCopied = 0;
            var toCopy = (int)Math.Min(count, _length - _position);
            while (toCopy > 0)
            {
                var index = _calculator.GetSegmentIndex(_position);
                var indexInSegment = _calculator.GetIndexInSegment(_position);

                var segment = FindSegment(index);

                var bytesToRead = Math.Min(segment->Length - indexInSegment, toCopy);
                if (bytesToRead > 0)
                {
                    var segmentBuffer = segment->Buffer;
                    Native.MemcpyFromUnmanagedFunc(buffer, offset + alreadyCopied, segmentBuffer, indexInSegment, bytesToRead);
                    alreadyCopied += bytesToRead;
                    toCopy -= bytesToRead;
                    _position += bytesToRead;
                }
            }

            return alreadyCopied;
        }

        public override int ReadByte()
        {
            if (_position >= Length)
                return -1;

            var index = _calculator.GetSegmentIndex(_position);
            var currentSegment = FindSegment(index);
            var currentSegmentIndex = _calculator.GetIndexInSegment(_position);

            _position += 1;

            var buffer = currentSegment->Buffer;
            return buffer[currentSegmentIndex];
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var bytesLeft = _capacity - _position;
            var bytesToAlloc = count - bytesLeft;
            if (bytesToAlloc > 0)
            {
                var numberOfSegments = _calculator.GetSegmentIndex(bytesToAlloc) + 1;
                AddSegments(numberOfSegments);
            }

            // find initial segment to write
            var index = _calculator.GetSegmentIndex(_position);

            var currentSegment = FindSegment(index);

            // intial segment selected, do writing
            var toWrite = count;
            do
            {
                var currentSegmentIndex = _calculator.GetIndexInSegment(_position);
                var segmentBuffer = currentSegment->Buffer;
                var spaceToWrite = currentSegment->Length - currentSegmentIndex;

                spaceToWrite = spaceToWrite > toWrite ? toWrite : spaceToWrite;
                if (spaceToWrite > 0)
                {
                    Native.MemcpyToUnmanagedFunc(segmentBuffer, currentSegmentIndex, buffer, offset, spaceToWrite);
                }

                toWrite -= spaceToWrite;
                _position += spaceToWrite;
                offset += spaceToWrite;
                currentSegment = currentSegment->Next;
            } while (toWrite > 0);

            if (_position > _length)
            {
                _length = _position;
            }
        }

        public override void WriteByte(byte value)
        {
            var bytesTillTheStreamEnd = _length - _position;

            var bytesLeft = _capacity - _position;
            if (bytesLeft == 0)
            {
                // nothing was left before adding this byte, it should be written at the beginning of the newSegment, change the position
                var newSegment = AddSegments(1);
                var array = newSegment->Buffer;
                array[0] = value;
                _position += 1;
            }
            else
            {
                // a more like usual Write path, find segment and index, write to the array directly, change the position
                var index = _calculator.GetSegmentIndex(_position);
                var currentSegment = FindSegment(index);
                var currentSegmentIndex = _calculator.GetIndexInSegment(_position);

                var arraySegment = currentSegment->Buffer;
                arraySegment[currentSegmentIndex] = value;
                _position += 1;
            }

            if (bytesTillTheStreamEnd == 0)
            {
                _length += 1;
            }
        }

        //public new void CopyTo(Stream destination)
        //{
        //    if (destination == null)
        //        throw new ArgumentNullException("destination");
        //    if (!CanRead && !CanWrite)
        //        throw new ObjectDisposedException(null, "Stream is closed");
        //    if (!destination.CanRead && !destination.CanWrite)
        //        throw new ObjectDisposedException("destination", "Stream is closed");
        //    if (!destination.CanWrite)
        //        throw new NotSupportedException("Cannot write to destination. It's unwritable.");

        //    // TODO: if other stream is Segment-based, optimize maybe?
        //    var toCopy = (int) _length - _position;

        //    var copier = new byte[_pool.SegmentSize];

        //    while (toCopy > 0)
        //    {
        //        var index = _pool.GetSegmentIndex(_position);
        //        var indexInSegment = _pool.GetIndexInSegment(_position);

        //        var segment = FindSegment(index);

        //        var bytesToRead = (int)Math.Min(_pool.SegmentSize - indexInSegment, toCopy);
        //        if (bytesToRead > 0)
        //        {
        //            var arraySegment = segment->Buffer;
        //            destination.Write(arraySegment.Array, arraySegment.Offset + indexInSegment, bytesToRead);
        //            toCopy -= bytesToRead;
        //            _position += bytesToRead;
        //        }
        //    }
        //}

        private Segment* FindSegment(int index)
        {
            //TODO: possibly read from memoized position in midpoint?
            // for small streams (less than 8kb) is worthless as getting the 2nd is one hop from head
            // for extremely long, this may save the day
            var currentSegment = _head;
            for (var i = 0; i < index; i++)
            {
                currentSegment = currentSegment->Next;
            }
            return currentSegment;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { return _length; }
        }

        public override long Position
        {
            get
            {
                return _position;
            }
            set
            {
                if (value < 0 || value > Length)
                    throw new ArgumentOutOfRangeException("value");
                _position = value;
            }
        }

        public override void Flush()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (_head != null)
            {
                _pool.Push(_head);
            }

            base.Dispose(disposing);
        }
    }
}