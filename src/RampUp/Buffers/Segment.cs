using System;
using System.Runtime.InteropServices;

namespace RampUp.Buffers
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public unsafe struct Segment
    {
        public const int Size = 32;
        // the fields offets ready to cast segment without reconstrucing to get ArraySegment like struct
        [FieldOffset(0)]
        public readonly byte* Buffer;
        [FieldOffset(12)]
        public readonly int Length;
        [FieldOffset(16)]
        public Segment* Next;

        public Segment(byte* buffer, int length)
        {
            Buffer = buffer;
            Length = length;
            Next = null;
        }

        public override string ToString()
        {
            return string.Format("Segment {0}", ((IntPtr)Buffer).ToInt64());
        }

        /// <summary>
        /// Gets the last segment of the possible list of segments represented by this, returning null if this is the tail.
        /// </summary>
        internal Segment* Tail
        {
            get
            {
                if (Next == null)
                {
                    return null;
                }

                var tail = Next;
                while (tail->Next != null)
                {
                    tail = tail->Next;
                }
                return tail;
            }
        }
    }
}