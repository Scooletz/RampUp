using RampUp.Buffers;

namespace RampUp.Ring
{
    /// <summary>
    /// A delegate representing a function processing a message.
    /// </summary>
    /// <param name="messageTypeId">The type of the message id.</param>
    /// <param name="chunk">The byte chunk containing the message.</param>
    public delegate void MessageHandler(int messageTypeId, ByteChunk chunk);
}