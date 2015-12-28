using System;
using RampUp.Atomics;

namespace RampUp.Buffers
{
    public interface IUnsafeBuffer : IDisposable
    {
        int Size { get; }
        unsafe byte* RawBytes { get; }
        AtomicLong GetAtomicLong(long index);
        AtomicInt GetAtomicInt(long index);
        void Write(int offset, ByteChunk chunk);
        void ZeroMemory(int start, int length);
    }
}