using System.Threading;

namespace RampUp.Actors
{
    /// <summary>
    /// The actors identifier used in the system as actors identity.
    /// </summary>
    public struct ActorId
    {
        private static long _next = 1;
        internal readonly long Value;

        public static ActorId Generate()
        {
            return new ActorId(Interlocked.Increment(ref _next));
        }

        private ActorId(long value)
        {
            Value = value;
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