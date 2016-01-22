namespace RampUp.Buffers
{
    /// <summary>
    /// A common denominator for <see cref="byte"/> array and <see cref="ByteChunk"/>.
    /// </summary>
    internal unsafe struct ByteSlice
    {
        private readonly byte[] _buffer;
        private readonly int _offset;
        public readonly int Count;
        private readonly byte* _chunk;

        public ByteSlice(byte[] buffer, int offset, int count)
        {
            _buffer = buffer;
            _offset = offset;
            Count = count;
            _chunk = null;
        }

        public ByteSlice(ByteChunk chunk)
        {
            _chunk = chunk.Pointer;
            _offset = 0;
            Count = chunk.Length;
            _buffer = null;
        }

        public void CopyFrom(int alreadyCopied, byte* segmentBuffer, int indexInSegment, int bytesToRead)
        {
            if (_buffer != null)
            {
                Native.MemcpyFromUnmanaged(_buffer, _offset + alreadyCopied, segmentBuffer, indexInSegment, bytesToRead);
            }
            else
            {
                Native.MemcpyUnmanaged(_chunk, segmentBuffer + indexInSegment, bytesToRead);
            }
        }

        public void CopyTo(byte* segmentBuffer, int currentSegmentIndex, int spaceToWrite, int additionalOffset)
        {
            if (_buffer != null)
            {
                Native.MemcpyToUnmanaged(segmentBuffer, currentSegmentIndex, _buffer, _offset + additionalOffset,
                    spaceToWrite);
            }
            else
            {
                Native.MemcpyUnmanaged(segmentBuffer + currentSegmentIndex, _chunk + additionalOffset, spaceToWrite);
            }
        }
    }
}