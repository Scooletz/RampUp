using System.Reflection;
using System.Reflection.Emit;

namespace RampUp.Actors.Impl
{
    public static class Reflector
    {
        private const int BeforeFieldsCount = Native.CacheLineSize/Native.SmallestPossibleObjectReferenceSize;

        /// <summary>
        /// Padds the type, using the same approach as <see cref="http://github.com/Scooletz/Padded"/>
        /// </summary>
        /// <param name="builder"></param>
        public static void PadBefore(this TypeBuilder builder)
        {
            for (var i = 0; i < BeforeFieldsCount; i++)
            {
                builder.DefineField("$padded" + i, typeof (object), FieldAttributes.Private);
            }
        }

        public static void PadAfter(this TypeBuilder builder)
        {
            for (var i = 0; i < Native.CacheLineSize; i++)
            {
                builder.DefineField("$padded" + (i + BeforeFieldsCount), typeof (object), FieldAttributes.Private);
            }
        }
    }
}