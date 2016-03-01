using System;

namespace RampUp.Tests.Buffers
{
    public static class Mocks
    {
        public static IAtomicInt AtomicInt = null;
        public static IAtomicLong AtomicLong = null;

        public interface IAtomicLong
        {
            long Read(IntPtr ptr);
            void Write(IntPtr ptr, long value);
            long VolatileRead(IntPtr ptr);
            void VolatileWrite(IntPtr ptr, long value);
            long CompareExchange(IntPtr ptr, long value, long comparand);
        }

        public interface IAtomicInt
        {
            int Read(IntPtr ptr);
            void Write(IntPtr ptr, int value);
            int VolatileRead(IntPtr ptr);
            void VolatileWrite(IntPtr ptr, int value);
            long CompareExchange(IntPtr ptr, int value, int comparand);
        }
    }
}