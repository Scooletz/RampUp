using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Padded.Fody;

namespace RampUp.Actors.Impl
{
    /// <summary>
    /// A very fast lookup based on types as keys.
    /// </summary>
    [Padded]
    public sealed class LongLookup<TValue>
        where TValue : struct
    {
        private const int PadWithElements = 16;
        private const int NotAssignedValue = 0;

        private readonly long[] _keys;
        private readonly TValue[] _values;
        private readonly int _length;
        private readonly int _lengthMask;

        public LongLookup(long[] keys, TValue[] values)
        {
            if (keys.Any(k => k <= 0))
            {
                throw new ArgumentException("Only positive keys allowed");
            }

            _length = 1 << (keys.Length.Log2() + 1);
            _lengthMask = _length - 1;
            _keys = new long[_length + PadWithElements*2];
            _values = new TValue[_length + PadWithElements*2];

            for (var i = 0; i < keys.Length; i++)
            {
                Put(keys[i], values[i]);
            }
        }

        private void Put(long key, TValue value)
        {
            var hash = Hash(key);
            var startIndex = hash & _lengthMask;
            for (var i = 0; i < _length; i++)
            {
                var index = ((startIndex + i) & _lengthMask) + PadWithElements;
                if (_keys[index] == NotAssignedValue)
                {
                    _keys[index] = key;
                    _values[index] = value;
                    return;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Hash(long l)
        {
            var a = (int) (l & (l >> 16));
            a -= a << 6;
            a ^= a >> 17;
            a -= a << 9;
            a ^= a << 4;
            a -= a << 3;
            a ^= a << 10;
            a ^= a >> 15;
            return a;
        }

        public bool TryGet(long key, out TValue value)
        {
            var hash = Hash(key);
            var index = hash & _lengthMask;

            for (var i = 0; i < _length; i++)
            {
                var position = ((index + i) & _lengthMask) + PadWithElements;
                if (_keys[position] == key)
                {
                    value = _values[position];
                    return true;
                }
            }

            value = default(TValue);
            return false;
        }

        public TValue GetOrDefault(long key)
        {
            var hash = Hash(key);
            var index = hash & _lengthMask;

            for (var i = 0; i < _length; i++)
            {
                var position = ((index + i) & _lengthMask) + PadWithElements;
                if (_keys[position] == key)
                {
                    return _values[position];
                }
            }

            return default(TValue);
        }
    }
}