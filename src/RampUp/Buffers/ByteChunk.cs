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

        public bool Equals(ByteChunk other)
        {
            return Pointer == other.Pointer && Length == other.Length;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ByteChunk && Equals((ByteChunk)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (int)Pointer * 397 ^ Length;
            }
        }

        public override string ToString()
        {
            return $"{new IntPtr(Pointer)} / {Length}";
        }
    }
}