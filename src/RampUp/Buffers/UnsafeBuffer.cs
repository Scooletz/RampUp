using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using RampUp.Atomics;

namespace RampUp.Buffers
{
    /// <summary>
    /// Unsafe, aligned to a page boundary buffer allocated with <see cref="Native.VirtualAlloc"/>.
    /// </summary>
    public sealed unsafe class UnsafeBuffer : IUnsafeBuffer
    {
        private readonly UIntPtr _allocated;

        public UnsafeBuffer(int size)
        {
            Size = size;
            var size1 = (UIntPtr) size.AlignToMultipleOf((int) Native.Info.AllocationGranularity);

            _allocated = Native.VirtualAlloc(UIntPtr.Zero, size1,
                Native.AllocationType.Commit | Native.AllocationType.Reserve,
                Native.MemoryProtection.ReadWrite);
            RawBytes = (byte*) _allocated;
        }

        public int Size { get; }

        public byte* RawBytes { get; }

        public AtomicLong GetAtomicLong(long index)
        {
            return new AtomicLong(RawBytes + index);
        }

        public AtomicInt GetAtomicInt(long index)
        {
            return new AtomicInt(RawBytes + index);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }

            var success = Native.VirtualFree(_allocated, UIntPtr.Zero, Native.AllocationType.Release);

            if (success == false)
            {
                var error = Marshal.GetLastWin32Error();
                if (error != 0)
                {
                    throw new Win32Exception(error);
                }
            }
        }

        ~UnsafeBuffer()
        {
            Dispose(false);
        }

        public void Write(int offset, ByteChunk chunk)
        {
            Native.MemcpyUnmanaged(RawBytes + offset, chunk.Pointer, chunk.Length);
        }

        public void ZeroMemory(int start, int length)
        {
            Native.ZeroMemory(RawBytes + start, length);
        }
    }
}