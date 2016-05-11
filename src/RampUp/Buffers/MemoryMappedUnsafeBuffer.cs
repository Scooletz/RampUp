using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Security.AccessControl;
using System.Security.Principal;
using RampUp.Atomics;

namespace RampUp.Buffers
{
    public sealed unsafe class MemoryMappedUnsafeBuffer : IUnsafeBuffer
    {
        private const int SizeOffset = sizeof(long);
        private readonly MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _accessor;
        private IntPtr _ptr;

        public static MemoryMappedUnsafeBuffer Create(int size, string name)
        {
            return new MemoryMappedUnsafeBuffer(size, name);
        }

        public static MemoryMappedUnsafeBuffer OpenExisting(string name)
        {
            return new MemoryMappedUnsafeBuffer(name);
        }

        private MemoryMappedUnsafeBuffer(string name)
        {
            _mmf = MemoryMappedFile.OpenExisting(name, MemoryMappedFileRights.ReadWrite);
            int size;

            using (var sizeView = GetSizeView(_mmf))
            {
                size = (int)sizeView.ReadInt64(0);
            }

            if (size <= 0)
            {
                throw new Exception($"The read size is {size}. It should be a positive int.");
            }

            Init(size);
        }

        private MemoryMappedUnsafeBuffer(int size, string name)
        {
            var actualSize = size + SizeOffset;

            var security = new MemoryMappedFileSecurity();
            var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            security.AddAccessRule(new AccessRule<MemoryMappedFileRights>(sid, MemoryMappedFileRights.ReadWriteExecute,
                AccessControlType.Allow));

            _mmf = MemoryMappedFile.CreateNew(name, actualSize, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, security, HandleInheritability.None);
            using (var sizeView = GetSizeView(_mmf))
            {
                sizeView.Write(0, (long)size);
                sizeView.Flush();
            }

            Init(size);
        }

        private static MemoryMappedViewAccessor GetSizeView(MemoryMappedFile memoryMappedFile)
        {
            return memoryMappedFile.CreateViewAccessor(0, SizeOffset);
        }

        private void Init(int size)
        {
            _accessor = _mmf.CreateViewStream(SizeOffset, size);
            _ptr = _accessor.SafeMemoryMappedViewHandle.DangerousGetHandle();

            Size = size;
            RawBytes = (byte*)_ptr;
        }

        public void Dispose()
        {
            _accessor.Dispose();
            _mmf.Dispose();
        }

        public int Size { get; private set; }
        public byte* RawBytes { get; private set; }

        public AtomicLong GetAtomicLong(long index)
        {
            return new AtomicLong(RawBytes + index);
        }

        public AtomicInt GetAtomicInt(long index)
        {
            return new AtomicInt(RawBytes + index);
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