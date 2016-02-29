using RampUp.Buffers;

namespace RampUp.Ring
{
    /// <summary>
    /// Ring buffer represents a FIFO for exchanging byte encoded messages. Depending on the implementation it might be thread-safe.
    /// </summary>
    public interface IRingBuffer
    {
        int Capacity { get; }
        int MaximumMessageLength { get; }
        bool Write(int messageTypeId, ByteChunk chunk);
        int Read(MessageHandler handler, int messageProcessingLimit);
    }
}