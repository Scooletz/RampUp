using System;
using System.Diagnostics.Contracts;
using System.Threading;
// ReSharper disable UnusedMember.Local

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
#if NOTESTS
        [Pure]
        public int Read()
        {
            return *_ptr;
        }

        public void Write(int value)
        {
            *_ptr = value;
        }

        [Pure]
        public int VolatileRead()
        {
            return Volatile.Read(ref *_ptr);
        }

        public void VolatileWrite(int value)
        {
            Volatile.Write(ref *_ptr, value);
        }

        [Pure]
        public long CompareExchange(int value, int comparand)
        {
            return Interlocked.CompareExchange(ref *_ptr, value, comparand);
        }
#else
        [Pure]
        public int Read()
        {
            return Mocks.AtomicInt.Read((IntPtr)_ptr);
        }

        public void Write(int value)
        {
            Mocks.AtomicInt.Write((IntPtr)_ptr, value);
        }

        [Pure]
        public int VolatileReads()
        {
            return Mocks.AtomicInt.VolatileRead((IntPtr)_ptr);
        }

        public void VolatileWrite(int value)
        {
            Mocks.AtomicInt.VolatileWrite((IntPtr)_ptr, value);
        }

        [Pure]
        public long CompareExchange(int value, int comparand)
        {
            return Mocks.AtomicInt.CompareExchange((IntPtr)_ptr, value, comparand);
        }
#endif
        public override string ToString()
        {
            return $"Under address {(IntPtr)_ptr} stores value {*_ptr}";
        }
    }
}