using System;
using RampUp.Buffers;

namespace RampUp.Actors
{
    /// <summary>
    /// The service which inializes and provides wall ahead log. It's a local, always appending file.
    /// </summary>
    public interface ILogFileService : IService
    {
        /// <summary>
        /// Appends the data to the log, returning the logical identifier of the operation for further correlation.
        /// </summary>
        /// <param name="data">The data to be appended.</param>
        /// <returns>The logical id of the operation for further correlation.</returns>
        long Append(ReadonlySegmentStream.Payload data);

        /// <summary>
        /// Registers handler of successfull data appending.
        /// </summary>
        /// <param name="onDataAppended"></param>
        void OnDataAppended(Action<DataAppended> onDataAppended);
    }


    public struct DataAppended
    {
        /// <summary>
        /// The operation id previously obtained when calling <see cref="ILogFileService.Append"/>.
        /// </summary>
        public readonly long OperationId;

        /// <summary>
        /// The logical position with a file, which might be used for refering to the given entry.
        /// </summary>
        public readonly long LogicalPosition;

        public DataAppended(long operationId, long logicalPosition)
        {
            OperationId = operationId;
            LogicalPosition = logicalPosition;
        }
    }
}