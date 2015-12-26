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

        public delegate void MemcpyFromUnmanaged(byte[] dest, int destIndex, byte* src, int srcIndex, int len);

        public delegate void MemcpyToUnmanaged(byte* pDest, int destIndex, byte[] src, int srcIndex, int len);

        public delegate void MemcpyUnmanaged (byte* dest, byte* src, int len);

        public static readonly MemcpyFromUnmanaged MemcpyFromUnmanagedFunc;
        public static readonly MemcpyToUnmanaged MemcpyToUnmanagedFunc;
        public static readonly MemcpyUnmanaged MemcpyUnmanagedFunc;
        public static readonly SystemInfo Info;

        static Native()
        {
            var bufferMemCpyMethods = typeof (Buffer)
                .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(mi => mi.Name == "Memcpy").ToArray();

            MemcpyFromUnmanagedFunc =
                (MemcpyFromUnmanaged)
                    Delegate.CreateDelegate(typeof (MemcpyFromUnmanaged), bufferMemCpyMethods.Single(mi =>
                    {
                        var parameters = mi.GetParameters();
                        return parameters.Length == 5 && parameters[0].ParameterType == typeof (byte[]);
                    }));

            MemcpyToUnmanagedFunc =
              (MemcpyToUnmanaged)
                  Delegate.CreateDelegate(typeof(MemcpyToUnmanaged), bufferMemCpyMethods.Single(mi =>
                  {
                      var parameters = mi.GetParameters();
                      return parameters.Length == 5 && parameters[0].ParameterType == typeof(byte*);
                  }));

            MemcpyUnmanagedFunc =
                (MemcpyUnmanaged)
                    Delegate.CreateDelegate(typeof (MemcpyUnmanaged),
                        bufferMemCpyMethods.Single(mi => mi.GetParameters().Length == 3));

            GetSystemInfo(out Info);
        }
    }
}