using System;
using System.IO;
using System.Threading;

namespace RampUp.Buffers
{
    public sealed class Segment
    {
        private readonly ArraySegment<byte> _segment;
        private Segment _next;

        private Segment(ArraySegment<byte> segment)
        {
            _segment = segment;
        }

        public override string ToString()
        {
            return string.Format("Segment offset {0}", _segment.Offset);
        }

        public sealed class Pool
        {
            public readonly int SegmentSize;
            private readonly int _fastDivideHelper;
            private readonly int _inSegmentMask;
            private volatile Segment _head;

            public Pool(int numberOfSegments, int segmentSize = 4096)
            {
                SegmentSize = segmentSize;
                if (segmentSize < 1024)
                {
                    throw new ArgumentException("Segment size should be reasonable, at least 1024", "segmentSize");
                }
                if (segmentSize.IsPowerOfTwo() == false)
                {
                    throw new ArgumentException("The segment of size should be power of two", "segmentSize");
                }

                _fastDivideHelper = segmentSize.Log2();
                _inSegmentMask = segmentSize - 1;

                var bytes = new byte[numberOfSegments * segmentSize];

                Segment head = null;
                for (var i = 0; i < numberOfSegments; i++)
                {
                    var newSegment = new Segment(new ArraySegment<byte>(bytes, i * segmentSize, segmentSize))
                    {
                        _next = head
                    };

                    head = newSegment;
                }

                _head = head;
            }

            public bool TryPop(out Segment result)
            {
                var head = _head;
                if (head == null)
                {
                    result = default(Segment);
                    return false;
                }
                if (Interlocked.CompareExchange(ref _head, head._next, head) == head)
                {
                    result = head;
                    result._next = null; // clear next before leaving
                    return true;
                }

                return TryPopImpl(out result);
            }

            private bool TryPopImpl(out Segment result)
            {
                Segment retrievedSegment;

                if (TryPop(1, out retrievedSegment) == 1)
                {
                    result = retrievedSegment;
                    result._next = null; // clear next before leaving
                    return true;
                }

                result = default(Segment);
                return false;
            }

            public int TryPop(int numberOfSegmentsToRetrieve, out Segment startingSegment)
            {
                var wait = new SpinWait();
                while (true)
                {
                    var head = _head;
                    // Is the stack empty?
                    if (head == null)
                    {
                        startingSegment = null;
                        return 0;
                    }

                    var next = head;
                    var count = 1;
                    for (; count < numberOfSegmentsToRetrieve && next._next != null; count++)
                    {
                        next = next._next;
                    }

                    if (Interlocked.CompareExchange(ref _head, next._next, head) == head)
                    {
                        startingSegment = head;
                        next._next = null; // clear popped sequence
                        return count;
                    }

                    wait.SpinOnce();
                }
            }

            public void Push(Segment segment)
            {
                var headToBePushed = segment;
                var tailToBePushed = segment.Tail;

                tailToBePushed._next = _head;
                if (Interlocked.CompareExchange(ref _head, headToBePushed, tailToBePushed._next) == tailToBePushed._next)
                {
                    return;
                }

                PushImpl(headToBePushed, tailToBePushed);
            }

            private void PushImpl(Segment head, Segment tail)
            {
                var spin = new SpinWait();

                do
                {
                    spin.SpinOnce();
                    tail._next = _head;
                }
                while (Interlocked.CompareExchange(ref _head, head, tail._next) != tail._next);
            }

            internal int GetSegmentIndex(long position)
            {
                return (int)(position >> _fastDivideHelper);
            }

            internal int GetIndexInSegment(long position)
            {
                return (int)(position & _inSegmentMask);
            }

            public System.IO.Stream GetStream()
            {
                return new Stream(this);
            }
        }

        private sealed class Stream : System.IO.Stream
        {
            private readonly Pool _pool;
            private Segment _head = null;
            private Segment _tail = null;
            private long _length;
            private long _position;
            private long _capacity;

            internal Stream(Pool pool)
            {
                _pool = pool;
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
                    _pool.Push(_head);
                    _head = null;
                    _tail = null;
                    _length = 0;
                    _position = 0;
                    _capacity = 0;
                    return;
                }

                var currentNumberOfSegments = _pool.GetSegmentIndex(_length);
                var nextNumberOfSegments = _pool.GetSegmentIndex(value);

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
            private Segment AddSegments(int segmentsToObtain)
            {
                Segment segment;
                if (_pool.TryPop(segmentsToObtain, out segment) == segmentsToObtain)
                {
                    if (_head == null)
                    {
                        _head = segment;
                        _tail = segment.Tail;
                    }
                    else
                    {
                        // attach the new segment to the end O(1)
                        _tail._next = segment;
                        _tail = segment.Tail; // rewrite tail O(number of segments added)
                    }

                    _capacity += _pool.SegmentSize * segmentsToObtain;

                    return segment;
                }
                else
                {
                    // not enough elements taken, release and throw
                    _pool.Push(segment);
                    throw new Exception("Not enough memory in the pool");
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_position >= _length)
                    return 0;

                var alreadyCopied = 0;
                var toCopy = (int)Math.Min(count, _length - _position);
                while (toCopy > 0)
                {
                    var index = _pool.GetSegmentIndex(_position);
                    var indexInSegment = _pool.GetIndexInSegment(_position);

                    var segment = FindSegment(index);

                    var bytesToRead = Math.Min(_pool.SegmentSize - indexInSegment, toCopy);
                    if (bytesToRead > 0)
                    {
                        var arraySegment = segment._segment;
                        Buffer.BlockCopy(arraySegment.Array, arraySegment.Offset + indexInSegment, buffer, offset + alreadyCopied, bytesToRead);
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

                var index = _pool.GetSegmentIndex(_position);
                var currentSegment = FindSegment(index);
                var currentSegmentIndex = _pool.GetIndexInSegment(_position);

                _position += 1;

                var arraySegment = currentSegment._segment;
                return arraySegment.Array[arraySegment.Offset + currentSegmentIndex];
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                var bytesTillTheStreamEnd = _capacity - _position;

                var bytesLeft = _capacity - _position;
                var bytesToAlloc = count - bytesLeft;
                if (bytesToAlloc > 0)
                {
                    var numberOfSegments = _pool.GetSegmentIndex(bytesToAlloc) + 1;
                    AddSegments(numberOfSegments);
                }

                // find initial segment to write
                var index = _pool.GetSegmentIndex(_position);

                var currentSegment = FindSegment(index);

                // intial segment selected, do writing
                var currentSegmentIndex = _pool.GetIndexInSegment(_position);
                var toWrite = count;
                do
                {
                    var spaceToWrite = _pool.SegmentSize - currentSegmentIndex;
                    var arraySegment = currentSegment._segment;

                    spaceToWrite = spaceToWrite > toWrite ? toWrite : spaceToWrite;
                    if (spaceToWrite > 0)
                    {
                        Buffer.BlockCopy(buffer, offset, arraySegment.Array, arraySegment.Offset, spaceToWrite);
                    }

                    toWrite -= spaceToWrite;
                    _position += spaceToWrite;
                    currentSegment = currentSegment._next;
                    currentSegmentIndex = 0;
                } while (toWrite > 0);

                if (count > bytesTillTheStreamEnd)
                {
                    _length += count - bytesTillTheStreamEnd;
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
                    var array = newSegment._segment.Array;
                    var offset = newSegment._segment.Offset;
                    array[offset] = value;
                    _position += 1;
                }
                else
                {
                    // a more like usual Write path, find segment and index, write to the array directly, change the position
                    var index = _pool.GetSegmentIndex(_position);
                    var currentSegment = FindSegment(index);
                    var currentSegmentIndex = _pool.GetIndexInSegment(_position);

                    var arraySegment = currentSegment._segment;
                    arraySegment.Array[arraySegment.Offset + currentSegmentIndex] = value;
                    _position += 1;
                }

                if (bytesTillTheStreamEnd == 0)
                {
                    _length += 1;
                }
            }

            private Segment FindSegment(int index)
            {
                //TODO: possibly read from memoized position in midpoint?
                // for small streams (less than 8kb) is worthless as getting the 2nd is one hop from head
                // for extremely long, this may save the day
                var currentSegment = _head;
                for (var i = 0; i < index; i++)
                {
                    currentSegment = currentSegment._next;
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

        /// <summary>
        /// Gets the last segment of the possible list of segments represented by this.
        /// </summary>
        internal Segment Tail
        {
            get
            {
                var tail = this;
                while (tail._next != null)
                {
                    tail = tail._next;
                }
                return tail;
            }
        }
    }
}