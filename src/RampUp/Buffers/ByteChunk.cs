using System;

namespace RampUp.Buffers
{
    /// <summary>
    /// An <see cref="ArraySegment{T}"/> counterpart for unmanaged chunk of bytes.
    /// </summary>
    public unsafe struct ByteChunk
    {
        public readonly byte* Pointer;
        public readonly int Length;

        public ByteChunk(byte* pointer, int length)
        {
            Pointer = pointer;
            Length = length;
        }
    }
}