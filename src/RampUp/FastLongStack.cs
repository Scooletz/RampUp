using System;
using System.Runtime.InteropServices;

namespace RampUp
{
    public unsafe class FastLongStack : IDisposable
    {
        private const uint Size = ushort.MaxValue + 1;
        private static readonly UIntPtr SizeAsPtr = (UIntPtr)Size;

        private readonly UIntPtr _allocated;
        private readonly WinSList.Head* _head;

        public FastLongStack()
        {
            _allocated = NativeMethods.VirtualAlloc(UIntPtr.Zero, SizeAsPtr,
                NativeMethods.AllocationType.Commit | NativeMethods.AllocationType.Reserve,
                NativeMethods.MemoryProtection.ReadWrite);


            _head = (WinSList.Head*)_allocated;

            var entriesPerCacheLine = NativeMethods.CacheLineSize / sizeof(WinSList.Entry);

            var counter = 1;
            var bytes = (byte*)_allocated;
            for (var i = 0; i < entriesPerCacheLine; i++)
            {
                for (var j = 1; j < Size / NativeMethods.CacheLineSize; j++)
                {
                    var offset = NativeMethods.CacheLineSize * j + (i*sizeof(WinSList.Entry));
                    var entry = (WinSList.Entry*)(bytes + offset);

                    entry->Data = counter;
                    counter += 1;

                    WinSList.InterlockedPushEntrySList(_head, entry);
                }
            }
        }

        public bool TryPop(out Item* item)
        {
            var value = WinSList.InterlockedPopEntrySList(_head);
            item = (Item*)value;
            return item != null;
        }

        public void Push(Item* item)
        {
            WinSList.InterlockedPushEntrySList(_head, (WinSList.Entry*)item);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        [StructLayout(LayoutKind.Explicit, Size = NativeMethods.MemoryAllocationAlignment)]
        public struct Item
        {
            [FieldOffset(0)]
            public Item* Next;

            [FieldOffset(NativeMethods.PtrSize)]
            public readonly long Data;
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }

            NativeMethods.VirtualFree(_allocated, SizeAsPtr, NativeMethods.AllocationType.Decommit | NativeMethods.AllocationType.Release);
        }

        ~FastLongStack()
        {
            Dispose(false);
        }
    }
}