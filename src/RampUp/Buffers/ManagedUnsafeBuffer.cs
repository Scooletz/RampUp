using System;
using System.Runtime.InteropServices;
using RampUp.Atomics;

namespace RampUp.Buffers
{
    /// <summary>
    /// A managed <see cref="IUnsafeBuffer"/> allowing for translations between <see cref="ByteChunk"/> and <see cref="ArraySegment{Int8}"/> with <see cref="Translate"/>.
    /// </summary>
    public sealed unsafe class ManagedUnsafeBuffer : IUnsafeBuffer
    {
        private readonly byte[] _bytes;
        private GCHandle _handle;

        public ManagedUnsafeBuffer(int size)
        {
            Size = size;
            _bytes = new byte[size];
            _handle = GCHandle.Alloc(_bytes, GCHandleType.Pinned);
            RawBytes = (byte*) _handle.AddrOfPinnedObject();
        }

        ~ManagedUnsafeBuffer()
        {
            Dispose(false);
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
            _handle.Free();
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

        public void Write(int offset, ByteChunk chunk)
        {
            Native.MemcpyUnmanaged(RawBytes + offset, chunk.Pointer, chunk.Length);
        }

        public void ZeroMemory(int start, int length)
        {
            Native.ZeroMemory(RawBytes + start, length);
        }

        public ArraySegment<byte> Translate(ByteChunk chunk)
        {
            var diff = (int) (chunk.Pointer - RawBytes);
            if (diff < 0)
            {
                throw new ArgumentException($"The {nameof(chunk)} starts before this managed byte buffer", nameof(chunk));
            }
            if (chunk.Pointer + chunk.Length > RawBytes + Size)
            {
                throw new ArgumentException($"The {nameof(chunk)} ends after this managed byte buffer", nameof(chunk));
            }

            return new ArraySegment<byte>(_bytes, diff, chunk.Length);
        }
    }
}