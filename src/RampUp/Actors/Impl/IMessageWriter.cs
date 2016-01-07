using RampUp.Ring;

namespace RampUp.Actors.Impl
{
    public interface IMessageWriter
    {
        /// <summary>
        /// Writes the <paramref name="message"/> with <paramref name="envelope"/> to the <paramref name=""/>
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="envelope"></param>
        /// <param name="message"></param>
        /// <param name="receiver"></param>
        /// <returns></returns>
        bool Write<TMessage>(ref Envelope envelope, ref TMessage message, IRingBuffer receiver)
            where TMessage : struct;
    }
}