using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

namespace RampUp
{
    [SuppressUnmanagedCodeSecurity]
    public unsafe class Native
    {
        public const int MemoryAllocationAlignment = 16;
        public const int CacheLineSize = 64;
        public const int PtrSize = 8; //IntPtr.Size

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern UIntPtr VirtualAlloc(UIntPtr lpAddress, UIntPtr dwSize, AllocationType flAllocationType,
            MemoryProtection flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualFree(UIntPtr lpAddress, UIntPtr dwSize, AllocationType dwFreeType);

        [Flags]
        public enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        [Flags]
        public enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        [DllImport("kernel32.dll", SetLastError = false)]
        private static extern void GetSystemInfo(out SystemInfo info);

        public enum ProcessorArchitecture
        {
            X86 = 0,
            X64 = 9,
            @Arm = -1,
            Itanium = 6,
            Unknown = 0xFFFF,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SystemInfo
        {
            public readonly ProcessorArchitecture ProcessorArchitecture; // WORD
            public readonly uint PageSize; // DWORD
            public readonly IntPtr MinimumApplicationAddress; // (long)void*
            public readonly IntPtr MaximumApplicationAddress; // (long)void*
            public readonly IntPtr ActiveProcessorMask; // DWORD*
            public readonly uint NumberOfProcessors; // DWORD (WTF)
            public readonly uint ProcessorType; // DWORD
            public readonly uint AllocationGranularity; // DWORD
            public readonly ushort ProcessorLevel; // WORD
            public readonly ushort ProcessorRevision; // WORD
        }

        public delegate void MemcpyFromUnmanagedDelegate(byte[] dest, int destIndex, byte* src, int srcIndex, int len);

        public delegate void MemcpyToUnmanagedDelegate(byte* pDest, int destIndex, byte[] src, int srcIndex, int len);

        public delegate void MemcpyUnmanagedDelegate (byte* dest, byte* src, int len);

        public delegate void ZeroMemoryDelegate(byte* src, long len);

        public static readonly MemcpyFromUnmanagedDelegate MemcpyFromUnmanaged;
        public static readonly MemcpyToUnmanagedDelegate MemcpyToUnmanaged;
        public static readonly MemcpyUnmanagedDelegate MemcpyUnmanaged;
        public static readonly ZeroMemoryDelegate ZeroMemory;
        public static readonly SystemInfo Info;

        static Native()
        {
            var staticMethods = typeof (Buffer)
                .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).ToArray();

            var bufferMemCpyMethods = staticMethods
                .Where(mi => mi.Name == "Memcpy").ToArray();

            MemcpyFromUnmanaged =
                (MemcpyFromUnmanagedDelegate)
                    Delegate.CreateDelegate(typeof (MemcpyFromUnmanagedDelegate), bufferMemCpyMethods.Single(mi =>
                    {
                        var parameters = mi.GetParameters();
                        return parameters.Length == 5 && parameters[0].ParameterType == typeof (byte[]);
                    }));

            MemcpyToUnmanaged =
              (MemcpyToUnmanagedDelegate)
                  Delegate.CreateDelegate(typeof(MemcpyToUnmanagedDelegate), bufferMemCpyMethods.Single(mi =>
                  {
                      var parameters = mi.GetParameters();
                      return parameters.Length == 5 && parameters[0].ParameterType == typeof(byte*);
                  }));

            MemcpyUnmanaged =
                (MemcpyUnmanagedDelegate)
                    Delegate.CreateDelegate(typeof (MemcpyUnmanagedDelegate),
                        bufferMemCpyMethods.Single(mi => mi.GetParameters().Length == 3));

            ZeroMemory =
                (ZeroMemoryDelegate)
                    Delegate.CreateDelegate(typeof (ZeroMemoryDelegate),
                        staticMethods.Single(mi => mi.Name == "ZeroMemory"));
            
            GetSystemInfo(out Info);
        }
    }
}