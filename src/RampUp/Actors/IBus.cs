namespace RampUp.Actors
{
    /// <summary>
    /// The message bus for actors.
    /// </summary>
    public interface IBus
    {
        /// <summary>
        /// Publishes the message to all implementors of <see cref="IHandle{TMessage}"/>.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="msg">The message to be sent</param>
        void Publish<TMessage>(ref TMessage msg)
            where TMessage : struct;

        /// <summary>
        /// Sends the message to the given actor specified by the <paramref name="receiver"/>.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="msg">The message to be sent.</param>
        /// <param name="receiver">The identifier.</param>
        void Send<TMessage>(ref TMessage msg, ActorId receiver)
            where TMessage : struct;
    }
}