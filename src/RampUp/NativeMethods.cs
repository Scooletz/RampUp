using System;
using System.Runtime.InteropServices;
using System.Security;

namespace RampUp
{
    [SuppressUnmanagedCodeSecurity]
    public class NativeMethods
    {
        public const int MemoryAllocationAlignment = 16;
        public const int CacheLineSize = 64;
        public const int PtrSize = 8;//IntPtr.Size

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern UIntPtr VirtualAlloc(UIntPtr lpAddress, UIntPtr dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

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
        public static extern void GetSystemInfo(out SystemInfo Info);

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
            public ProcessorArchitecture ProcessorArchitecture; // WORD
            public uint PageSize; // DWORD
            public IntPtr MinimumApplicationAddress; // (long)void*
            public IntPtr MaximumApplicationAddress; // (long)void*
            public IntPtr ActiveProcessorMask; // DWORD*
            public uint NumberOfProcessors; // DWORD (WTF)
            public uint ProcessorType; // DWORD
            public uint AllocationGranularity; // DWORD
            public ushort ProcessorLevel; // WORD
            public ushort ProcessorRevision; // WORD
        }
    }

    public unsafe class WinSList
    {
        /// <summary>
        /// Initializes the head of a singly linked list.
        /// </summary>
        /// <param name="head">Head of the list to be initialied.</param>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern void InitializeSListHead(Head* head);

        /// <summary>
        /// Inserts an item at the front of a singly linked list. Access to the list is synchronized on a multiprocessor system.
        /// </summary>
        /// <param name="head">The list head.</param>
        /// <param name="entry">The entry to be pushed.</param>
        /// <returns>The return value is the previous first item in the list. If the list was previously empty, the return value is null.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern Entry* InterlockedPushEntrySList(Head* head, Entry* entry);

        /// <summary>
        /// Removes an item from the front of a singly linked list. Access to the list is synchronized on a multiprocessor system.
        /// </summary>
        /// <param name="head">The list head.</param>
        /// <returns>The return value is a pointer to the item removed from the list. If the list was previously empty, the return value is null.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern Entry* InterlockedPopEntrySList(Head* head);

        /// <summary>
        /// Removes all items from a singly linked list. Access to the list is synchronized on a multiprocessor system.
        /// </summary>
        /// <param name="head"></param>
        /// <returns>The return value is a pointer to the items removed from the list. If the list is empty, the return value is null.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern Entry* InterlockedFlushSList(Head* head);

        [StructLayout(LayoutKind.Explicit, Size = NativeMethods.MemoryAllocationAlignment)]
        public struct Entry
        {
            [FieldOffset(0)]
            public Entry* Next;

            [FieldOffset(NativeMethods.PtrSize)]
            public long Data;
        }

        [StructLayout(LayoutKind.Explicit, Size = NativeMethods.MemoryAllocationAlignment)]
        public struct Head
        {
        }
    }
}