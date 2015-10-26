using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;

namespace RampUp.Tests
{
    public class FastHeapTests
    {
        [Test]
        public unsafe void BasicPopPush()
        {
            var ptrs = new List<IntPtr>();
            var observed = new HashSet<long>();

            using (var heap = new FastLongStack())
            {
                FastLongStack.Item* item;
                while (heap.TryPop(out item))
                {
                    ptrs.Add((IntPtr)item);
                    if (observed.Add(item->Data) == false)
                    {
                        throw new InvalidOperationException("Already seen value.");
                    }
                }

                // go back
                foreach (var ptr in ptrs)
                {
                    heap.Push((FastLongStack.Item*)ptr);
                }
            }
        }

        [Test]
        public unsafe void PopPush()
        {
            const int count = 10000;

            using (var stack = new FastLongStack())
            {
                var item = default(FastLongStack.Item*);

                var sw = Stopwatch.StartNew();
                for (var i = 0; i < count; i++)
                {
                    FastLongStack.Item* next;
                    stack.TryPop(out next);
                    if (item != null)
                    {
                        item->Next = next;
                    }
                    item = next;
                }

                for (var i = 0; i < count; i++)
                {
                    var next = item->Next;
                    stack.Push(item);
                    item = next;
                }
                sw.Stop();

                Console.WriteLine("PopPush {0} took: {1}", count, sw.Elapsed);
            }
        }

        [Test]
        public void PopPushConcurrent()
        {
            var stack = new ConcurrentStack<long>();
            const int count = 10000;

            for (var i = 0; i < count; i++)
            {
                stack.Push(i);
            }

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < count; i++)
            {
                long next;
                stack.TryPop(out next);
            }

            for (var i = 0; i < count; i++)
            {
                stack.Push(i);
            }
            sw.Stop();

            Console.WriteLine("PopPush {0} took: {1}", count, sw.Elapsed);
        }
    }
}