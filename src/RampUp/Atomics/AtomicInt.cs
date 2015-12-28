using System;
using System.Threading;

namespace RampUp.Atomics
{
    /// <summary>
    /// A wrapper around an address under which int is stored. Besides <see cref="Read"/>, <see cref="Write"/> provides threadsafe access 
    /// with possible Aquire-Release Fence semantics.
    /// </summary>
    public unsafe struct AtomicInt
    {
        private readonly int* _ptr;

        public AtomicInt(byte* ptr)
        {
            if (((long)ptr & 3) != 0)
            {
                throw new ArgumentException("The address should be aligned to 4 byte boundary");
            }
            _ptr = (int*)ptr;
        }

        public int Read()
        {
            return *_ptr;
        }

        public void Write(int value)
        {
            *_ptr = value;
        }

        public int VolatileRead()
        {
            return Volatile.Read(ref *_ptr);
        }

        public void VolatileWrite(int value)
        {
            Volatile.Write(ref *_ptr, value);
        }

        public long CompareExchange(int value, int comparand)
        {
            return Interlocked.CompareExchange(ref *_ptr, value, comparand);
        }

        public override string ToString()
        {
            return $"Under address {(IntPtr)_ptr} stores value {*_ptr}";
        }
    }
}