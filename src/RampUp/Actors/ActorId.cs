using System;

namespace RampUp.Actors
{
    /// <summary>
    /// The actors identifier used in the system as actors identity.
    /// </summary>
    public struct ActorId
    {
        public const byte MaxValue = byte.MaxValue;
        internal readonly byte Value;

        public ActorId(byte value)
        {
            Value = value;
        }

        public ActorId GetNext()
        {
            if (Value == MaxValue)
            {
                throw new InvalidOperationException($"You can't have more than {MaxValue} actors.");
            }
            return new ActorId((byte) (Value + 1));
        }

        public bool Equals(ActorId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ActorId && Equals((ActorId) obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool Equal(ActorId id1, ActorId id2)
        {
            return id1.Value == id2.Value;
        }
    }
}