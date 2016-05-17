using System;
using RampUp.Buffers;

namespace RampUp.Ring
{
    /// <summary>
    /// Ring buffer represents a FIFO for exchanging byte encoded messages. Depending on the implementation it might be thread-safe.
    /// </summary>
    public interface IRingBuffer
    {
        /// <summary>
        /// The buffer capacity, how many bytes it can store
        /// </summary>
        int Capacity { get; }

        int MaximumMessageLength { get; }

        /// <summary>
        /// Tries to write a message.
        /// </summary>
        /// <param name="messageTypeId">The message type id.</param>
        /// <param name="chunk">The message payload</param>
        /// <returns>If the write succeeded.</returns>
        bool Write(int messageTypeId, ByteChunk chunk);

        /// <summary>
        /// Reads up to <paramref name="messageProcessingLimit"/> messages, calling <paramref name="handler"/> for each of them.
        /// </summary>
        /// <param name="handler">The message handler</param>
        /// <param name="messageProcessingLimit">The maximum number of messages to process.</param>
        /// <returns>The number of processed messages.</returns>
        int Read(MessageHandler handler, int messageProcessingLimit);

        /// <summary>
        /// Reads a raw chunk of messages, providing ability to 
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="maxSizeToProcess"></param>
        /// <returns></returns>
        int ReadRaw(RawMessageChunkHandler handler, int maxSizeToProcess);
    }
}