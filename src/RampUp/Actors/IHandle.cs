namespace RampUp.Actors
{
    /// <summary>
    /// The interface marking the actor as handling given message type.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    public interface IHandle<TMessage> : IActor
        where TMessage : struct
    {
        /// <summary>
        /// Handling methods
        /// </summary>
        /// <param name="envelope">The envelope of the handled message passed as a separate paramter to do not blow up the generic structs (it could have been an Envelope{T}).</param>
        /// <param name="msg">The message being passed as ref to eliminate copying the payload.</param>
        void Handle(ref Envelope envelope, ref TMessage msg);
    }

    public interface IActor
    {
    }

    /// <summary>
    /// An interface for an idle strategy being executed at the end of each batch of messages.
    /// </summary>
    public interface IIdleStrategy
    {
        void ProcessingBatchOfMessagesEnded(BatchInfo batch);
    }

    public struct BatchInfo
    {
        public int RequestedNumberOfMessages;
        public int DispatchedNumberOfMessages;

        public BatchInfo(int requestedNumberOfMessages, int dispatchedNumberOfMessages)
        {
            RequestedNumberOfMessages = requestedNumberOfMessages;
            DispatchedNumberOfMessages = dispatchedNumberOfMessages;
        }
    }
}