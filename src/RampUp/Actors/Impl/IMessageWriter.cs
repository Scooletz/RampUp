using RampUp.Buffers;

namespace RampUp.Actors.Impl
{
    public interface IMessageWriter
    {
        /// <summary>
        /// Writes the <paramref name="message"/> with <paramref name="envelope"/> to the <paramref name="write"/>
        /// </summary>
        /// <returns>If the write was successful, passing over just <see cref="WriteDelegate"/>.</returns>
        bool Write<TMessage>(ref Envelope envelope, ref TMessage message, WriteDelegate write)
            where TMessage : struct;
    }

    public delegate bool WriteDelegate(int messageTypeId, ByteChunk chunk);
}