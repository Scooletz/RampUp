using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RampUp.Actors.Impl
{
    public struct MessageMetadata
    {
        public readonly int Id;
        public readonly short Size;
        public readonly short EnvelopeOffset;

        public MessageMetadata(int id, short size, short envelopeOffset)
        {
            Id = id;
            Size = size;
            EnvelopeOffset = envelopeOffset;
        }

        public static LongLookup<MessageMetadata> BuildMetadata(IStructSizeCounter counter,
            Func<Type, int> messageIdGetter, Type[] structTypes)
        {
            var keys = structTypes.Select(GetKey).ToArray();

            var values =
                structTypes.Select(type => new MessageMetadata(messageIdGetter(type), (short) counter.GetSize(type),
                    (short) (int) Marshal.OffsetOf(type, Envelope.FieldName))).ToArray();

            var metadata = new LongLookup<MessageMetadata>(keys, values);
            return metadata;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetKey(Type t)
        {
            return GetKey(t.TypeHandle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetKey(RuntimeTypeHandle typeHandle)
        {
            return typeHandle.Value.ToInt64();
        }
    }
}