using System;
using System.IO;

namespace RampUp.Buffers
{
    /// <summary>
    /// The readonly version of <see cref="SegmentStream"/>, being reusable by filling the instance by calling <see cref="Fill"/>
    /// </summary>
    public unsafe class ReadonlySegmentStream : Stream
    {
        protected IndexCalculator Calculator;
        protected Segment* Head = null;
        protected int _length;
        protected int _position;
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _length;

        protected ReadonlySegmentStream(IndexCalculator calculator)
        {
            Calculator = calculator;
        }

        /// <summary>
        /// Initializes empty <see cref="ReadonlySegmentStream"/>, which should be filled with data.
        /// </summary>
        public ReadonlySegmentStream()
        {
        }

        /// <summary>
        /// The Payload of readonly stream.
        /// </summary>
        public struct Payload
        {
            public Segment* Head;
            public IndexCalculator Calculator;
            public int Length;
            public int Position;

            public Payload(Segment* head, IndexCalculator calculator, int length, int position)
            {
                Head = head;
                Calculator = calculator;
                Length = length;
                Position = position;
            }
        }

        public void Fill(ref Payload payload)
        {
            Head = payload.Head;
            Calculator = payload.Calculator;
            _length = payload.Length;
            _position = payload.Position;
        }

        public override long Position
        {
            get { return _position; }
            set
            {
                if (value < 0 || value > Length)
                    throw new ArgumentOutOfRangeException("value");
                _position = (int) value;
            }
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var slice = new ByteSlice(buffer, offset, count);
            return ReadImpl(ref slice);
        }

        public int Read(ByteChunk chunk)
        {
            var slice = new ByteSlice(chunk);
            return ReadImpl(ref slice);
        }

        private int ReadImpl(ref ByteSlice slice)
        {
            if (_position >= _length)
                return 0;

            var alreadyCopied = 0;
            var toCopy = Math.Min(slice.Count, _length - _position);
            while (toCopy > 0)
            {
                var index = Calculator.GetSegmentIndex(_position);
                var indexInSegment = Calculator.GetIndexInSegment(_position);

                var segment = FindSegment(index);

                var bytesToRead = Math.Min(segment->Length - indexInSegment, toCopy);
                if (bytesToRead > 0)
                {
                    var segmentBuffer = segment->Buffer;
                    slice.CopyFrom(alreadyCopied, segmentBuffer, indexInSegment, bytesToRead);
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

            var index = Calculator.GetSegmentIndex(_position);
            var currentSegment = FindSegment(index);
            var currentSegmentIndex = Calculator.GetIndexInSegment(_position);

            _position += 1;

            var buffer = currentSegment->Buffer;
            return buffer[currentSegmentIndex];
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Flush()
        {
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

        protected static Segment* GetTailOrThis(Segment* segment)
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

        protected Segment* FindSegment(int index)
        {
            //TODO: possibly read from memoized position in midpoint?
            // for small streams (less than 8kb) is worthless as getting the 2nd is one hop from head
            // for extremely long, this may save the day
            var currentSegment = Head;
            for (var i = 0; i < index; i++)
            {
                currentSegment = currentSegment->Next;
            }
            return currentSegment;
        }
    }
}