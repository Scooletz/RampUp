using System;
using System.Diagnostics.Contracts;
using System.Threading;
// ReSharper disable PureAttributeOnVoidMethod
// ReSharper disable UnusedMember.Local

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
        public long ReadImpl()
        {
            return *_ptr;
        }

        [Pure]
        public long Read()
        {
            return Mocks.AtomicLong.Read((IntPtr) _ptr);
        }

        [Pure]
        private void WriteImpl(long value)
        {
            *_ptr = value;
        }

        [Pure]
        public void Write(int value)
        {
            Mocks.AtomicLong.Write((IntPtr) _ptr, value);
        }

        [Pure]
        private long VolatileReadImpl()
        {
            return Volatile.Read(ref *_ptr);
        }

        [Pure]
        public long VolatileRead()
        {
            return Mocks.AtomicLong.VolatileRead((IntPtr) _ptr);
        }

        [Pure]
        private void VolatileWriteImpl(long value)
        {
            Volatile.Write(ref *_ptr, value);
        }

        [Pure]
        public void VolatileWrite(long value)
        {
            Mocks.AtomicLong.VolatileWrite((IntPtr) _ptr, value);
        }

        [Pure]
        private long CompareExchangeImpl(long value, long comparand)
        {
            return Interlocked.CompareExchange(ref *_ptr, value, comparand);
        }

        [Pure]
        public long CompareExchange(long value, long comparand)
        {
            return Mocks.AtomicLong.CompareExchange((IntPtr) _ptr, value, comparand);
        }

        public override string ToString()
        {
            return $"Under address {(IntPtr)_ptr} stores value {*_ptr}";
        }
    }
}