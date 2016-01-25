using System;
using RampUp.Buffers;

namespace RampUp.Actors.Impl.IO
{
    internal sealed class LogFileFileService : ILogFileService, IHandle<LogFile.AppendResponse>
    {
        private readonly IBus _bus;
        private readonly ISegmentPool _pool;
        private long _counter;
        private Action<DataAppended> _onDataAppended;

        public LogFileFileService(IBus bus, ISegmentPool pool)
        {
            _bus = bus;
            _pool = pool;
        }

        public long Append(ReadonlySegmentStream.Payload data)
        {
            var id = _counter++;

            var msg = new LogFile.AppendRequest(id, data);
            _bus.Publish(ref msg);
            return id;
        }

        public void OnDataAppended(Action<DataAppended> onDataAppended)
        {
            _onDataAppended = onDataAppended;
        }

        public unsafe void Handle(ref Envelope envelope, ref LogFile.AppendResponse msg)
        {
            _onDataAppended(new DataAppended(msg.OperationId, msg.LogicalPosition));
            _pool.Push(msg.ReturnedSegment);
        }
    }
}