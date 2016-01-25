using RampUp.Buffers;

namespace RampUp.Actors.Impl.IO
{
    internal static class LogFile
    {
        internal struct AppendRequest
        {
            public readonly long OperationId;
            public readonly ReadonlySegmentStream.Payload Data;

            public AppendRequest(long operationId, ReadonlySegmentStream.Payload data)
            {
                OperationId = operationId;
                Data = data;
            }
        }

        internal unsafe struct AppendResponse
        {
            public readonly long OperationId;
            public readonly long LogicalPosition;
            public readonly Segment* ReturnedSegment;

            public AppendResponse(long operationId, long logicalPosition, Segment* returnedSegment)
            {
                OperationId = operationId;
                LogicalPosition = logicalPosition;
                ReturnedSegment = returnedSegment;
            }
        }
    }
}