using System;
using System.Diagnostics.Contracts;
using System.Threading;
// ReSharper disable PureAttributeOnVoidMethod

namespace RampUp.Atomics
{
    /// <summary>
    /// A wrapper around an address under which long is stored. Besides <see cref="Read"/>, <see cref="Write"/> provides threadsafe access 
    /// with possible Aquire-Release Fence semantics.
    /// </summary>
    public unsafe struct AtomicLong
    {
        private readonly long* _ptr;

        public AtomicLong(byte* ptr)
        {
            if (((long)ptr & 7) != 0)
            {
                throw new ArgumentException("The address should be aligned to 8 byte boundary");
            }
            _ptr = (long*)ptr;
        }

        [Pure]
        public long Read()
        {
            return *_ptr;
        }

        [Pure]
        public void Write(long value)
        {
            *_ptr = value;
        }

        [Pure]
        public long VolatileRead()
        {
            return Volatile.Read(ref *_ptr);
        }

        [Pure]
        public void VolatileWrite(long value)
        {
            Volatile.Write(ref *_ptr, value);
        }

        [Pure]
        public long CompareExchange(long value, long comparand)
        {
            return Interlocked.CompareExchange(ref *_ptr, value, comparand);
        }

        public override string ToString()
        {
            return $"Under address {(IntPtr)_ptr} stores value {*_ptr}";
        }
    }
}