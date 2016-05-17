using RampUp.Buffers;
using static RampUp.Ring.RingBufferDescriptor;

namespace RampUp.Ring
{
    /// <summary>
    /// Represents a raw rawChunk of messages that can be read with
    /// </summary>
    public struct RawMessageChunk
    {
        private readonly ByteChunk _chunk;

        public RawMessageChunk(ByteChunk chunk)
        {
            _chunk = chunk;
        }

        /// <summary>
        /// Tries to read a message.
        /// </summary>
        /// <param name="rawChunk">The rawChunk to be read from.</param>
        /// <param name="handler">The handler to handle a read message</param>
        /// <param name="position">The value handling the position. Start reading with setting this to 0.</param>
        /// <returns>If another message should can be read.</returns>
        /// <remarks>
        /// Use it in a loop till the <see cref="TryReadMessage"/> returns true. This enables skipping creation of an iterator, as the only needed state is <paramref name="position"/>.
        /// </remarks>
        public static unsafe bool TryReadMessage(ref RawMessageChunk rawChunk, MessageHandler handler, ref int position)
        {
            if (position < 0 || position >= rawChunk._chunk.Length)
            {
                return false;
            }

            var header = *(long*) (rawChunk._chunk.Pointer + position);

            var recordLength = RecordLength(header);
            if (recordLength <= 0)
            {
                return false;
            }

            var delta = recordLength.AlignToMultipleOf(RecordAlignment);
            position += delta;

            var messageTypeId = MessageTypeId(header);

            handler(messageTypeId,
                new ByteChunk(rawChunk._chunk.Pointer + position + HeaderLength, recordLength - HeaderLength));
            position += delta;

            return true;
        }
    }
}