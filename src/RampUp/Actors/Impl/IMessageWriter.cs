using RampUp.Buffers;
using RampUp.Ring;

namespace RampUp.Actors.Impl
{
    public interface IMessageWriter
    {
        /// <summary>
        /// Writes the <paramref name="message"/> with <paramref name="envelope"/> to the <paramref name="bufferToWrite"/>
        /// </summary>
        /// <returns>If the write was successful, passing over just <see cref="IRingBuffer.Write"/>.</returns>
        bool Write<TMessage>(ref Envelope envelope, ref TMessage message, IRingBuffer bufferToWrite)
            where TMessage : struct;
    }
}