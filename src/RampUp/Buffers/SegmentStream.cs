using System;

namespace RampUp.Buffers
{
    /// <summary>
    /// A stream based on <see cref="ISegmentPool"/> using its segments as chunks to build up the stream.
    /// </summary>
    public sealed unsafe class SegmentStream : ReadonlySegmentStream
    {
        private readonly ISegmentPool _pool;
        private Segment* _tail = null;
        private int _capacity;

        internal SegmentStream(ISegmentPool pool) :
            base(new IndexCalculator(pool.SegmentLength))
        {
            _pool = pool;
        }

        public override void SetLength(long value)
        {
            if (value < 0)
            {
                throw new ArgumentException("Length must be greater than 0", "value");
            }

            if (value == 0)
            {
                if (Head != null)
                {
                    _pool.Push(Head);
                }

                Head = null;
                _tail = null;
                _length = 0;
                _position = 0;
                _capacity = 0;
                return;
            }

            var currentNumberOfSegments = Calculator.GetSegmentIndex(_length);
            var nextNumberOfSegments = Calculator.GetSegmentIndex(value);

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

            _length = (int) value;
            if (_position > value)
            {
                _position = (int) value;
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
                if (Head == null)
                {
                    Head = segment;
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

        public override void Write(byte[] buffer, int offset, int count)
        {
            var slice = new ByteSlice(buffer, offset, count);
            WriteImpl(ref slice);
        }

        public void Write(ByteChunk chunk)
        {
            var slice = new ByteSlice(chunk);
            WriteImpl(ref slice);
        }

        private void WriteImpl(ref ByteSlice slice)
        {
            var offset = 0;
            var bytesLeft = _capacity - _position;
            var bytesToAlloc = slice.Count - bytesLeft;
            if (bytesToAlloc > 0)
            {
                var numberOfSegments = Calculator.GetSegmentIndex(bytesToAlloc) + 1;
                AddSegments(numberOfSegments);
            }

            // find initial segment to write
            var index = Calculator.GetSegmentIndex(_position);

            var currentSegment = FindSegment(index);

            // intial segment selected, do writing
            var toWrite = slice.Count;
            do
            {
                var currentSegmentIndex = Calculator.GetIndexInSegment(_position);
                var segmentBuffer = currentSegment->Buffer;
                var spaceToWrite = currentSegment->Length - currentSegmentIndex;

                spaceToWrite = spaceToWrite > toWrite ? toWrite : spaceToWrite;
                if (spaceToWrite > 0)
                {
                    slice.CopyTo(segmentBuffer, currentSegmentIndex, spaceToWrite, offset);
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
                var index = Calculator.GetSegmentIndex(_position);
                var currentSegment = FindSegment(index);
                var currentSegmentIndex = Calculator.GetIndexInSegment(_position);

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
        //    var toCopy = (int) Length - _position;

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

        protected override void Dispose(bool disposing)
        {
            if (Head != null)
            {
                _pool.Push(Head);
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Wraps the whole content from this instance in a context object. This effectively means that no segments are returned to the pool but rather are passes by ref in the context.
        /// </summary>
        /// <param name="ctx">The context to be returned.</param>
        // ReSharper disable once RedundantAssignment
        public void WrapInContext(ref Payload ctx)
        {
            ctx = new Payload(Head, Calculator, _length, _position);

            Head = null;
            _tail = null;
            _length = 0;
            _position = 0;
            _capacity = 0;
        }

        public override bool CanWrite => true;
    }
}