using System;

namespace RampUp.Buffers
{
    public sealed unsafe class UnsafeBuffer : IDisposable
    {
        private readonly UIntPtr _size;
        private readonly UIntPtr _allocated;

        public UnsafeBuffer(int minimalSize)
        {
            var pageSize = Native.Info.AllocationGranularity;

            if (minimalSize % pageSize == 0)
            {
                _size = (UIntPtr)minimalSize;
            }
            else
            {
                // size different than allocation granularity, round up to the next multiple
                _size = (UIntPtr)((minimalSize / pageSize + 1) * pageSize);
            }

            _allocated = Native.VirtualAlloc(UIntPtr.Zero, _size,
              Native.AllocationType.Commit | Native.AllocationType.Reserve,
              Native.MemoryProtection.ReadWrite);
            RawBytes = (byte*)_allocated;
        }

        public byte* RawBytes { get; }

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

            Native.VirtualFree(_allocated, _size, Native.AllocationType.Decommit | Native.AllocationType.Release);
        }
    }
}