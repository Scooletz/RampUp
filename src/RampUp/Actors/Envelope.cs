namespace RampUp.Actors
{
    /// <summary>
    /// The envelope being sent with each message
    /// </summary>
    public struct Envelope
    {
        /// <summary>
        /// The sender.
        /// </summary>
        public readonly ActorId Sender;

        public Envelope(ActorId sender)
        {
            Sender = sender;
        }
    }
}