using System;
using System.Collections.Generic;
using System.Threading;
using RampUp.Buffers;

namespace RampUp.Actors.Impl.IO
{
    /// <summary>
    /// An internal actor handling all the IO, including features: <see cref="ILogFileService"/>.
    /// </summary>
    internal sealed unsafe class CompletionPortIdleStrategy : IDisposable, IIdleStrategy
    {
        private readonly IntPtr _port;
        private static readonly int EntriesCount = (256*256)/sizeof (Native.CompletionPorts.OverlappedEntry);

        private readonly UnsafeBuffer _buffer;
        private readonly Native.CompletionPorts.OverlappedEntry* _entries;

        private readonly Dictionary<IntPtr, Action<CompletedAction>> _handlers =
            new Dictionary<IntPtr, Action<CompletedAction>>();

        private readonly List<Action> _endOfDispatchActions = new List<Action>();

        public CompletionPortIdleStrategy()
        {
            _port = Native.CompletionPorts.CreateIoCompletionPort(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1);
            _buffer = new UnsafeBuffer(sizeof (Native.CompletionPorts.OverlappedEntry)*EntriesCount);
            _entries = (Native.CompletionPorts.OverlappedEntry*) _buffer.RawBytes;
        }

        public void Register(IntPtr handle, Action<CompletedAction> handler, Action endOfDispatchAction = null)
        {
            Native.CompletionPorts.CreateIoCompletionPort(handle, _port, handle, 1);
            _handlers[handle] = handler;

            if (endOfDispatchAction != null)
            {
                _endOfDispatchActions.Add(endOfDispatchAction);
            }
        }

        public struct CompletedAction
        {
            public readonly uint NumberOfTransferredBytes;
            public readonly NativeOverlapped* Overlapped;

            public CompletedAction(ref Native.CompletionPorts.OverlappedEntry entry)
            {
                NumberOfTransferredBytes = entry.NumberOfTransferredBytes;
                Overlapped = entry.Overlapped;
            }
        }

        public void Dispose()
        {
            Native.CloseHandle(_port);
            _buffer.Dispose();
        }

        public void ProcessingBatchOfMessagesEnded(BatchInfo batch)
        {
            uint count;
            if (Native.CompletionPorts.GetQueuedCompletionStatusEx(_port, _entries, (uint) EntriesCount,
                out count, 10, false) && count > 0)
            {
                for (var i = 0; i < count; i++)
                {
                    DispatchOverlappedResult(ref _entries[i]);
                }

                for (var i = 0; i < _endOfDispatchActions.Count; i++)
                {
                    _endOfDispatchActions[i]();
                }
            }
            else
            {
                // nothing happened, there are some requests to be resolved but nothing was resolved yet
                Thread.Sleep(0);
            }
        }

        private void DispatchOverlappedResult(ref Native.CompletionPorts.OverlappedEntry entry)
        {
            Action<CompletedAction> handler;
            if (_handlers.TryGetValue(entry.CompletionKey, out handler))
            {
                handler(new CompletedAction(ref entry));
            }
        }
    }
}