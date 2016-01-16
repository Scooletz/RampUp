using System.Runtime.InteropServices;
using RampUp.Ring;

namespace RampUp.Actors
{
    /// <summary>
    /// The envelope being sent with each message
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = RingBufferDescriptor.RecordAlignment)]
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