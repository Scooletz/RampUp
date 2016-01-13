using System.Runtime.InteropServices;

namespace RampUp.Actors
{
    /// <summary>
    /// The envelope being sent with each message
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = Native.CacheLineSize)]
    public struct Envelope
    {
        /// <summary>
        /// The sender.
        /// </summary>
        [FieldOffset(0)]
        public readonly ActorId Sender;

        public Envelope(ActorId sender)
        {
            Sender = sender;
        }
    }
}