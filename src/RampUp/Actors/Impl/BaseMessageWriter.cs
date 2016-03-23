using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using RampUp.Buffers;

namespace RampUp.Actors.Impl
{
    public abstract class BaseMessageWriter
    {
        private IntLookup<MessageMetadata> _metadata;
        private long _typePointerDiff;

        protected struct MessageMetadata
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
        }

        protected void Init(IStructSizeCounter counter, Func<Type, int> messageIdGetter, Type[] structTypes)
        {
            var typePointers = structTypes.Select(t => t.TypeHandle.Value.ToInt64()).ToArray();
            _typePointerDiff = typePointers.Min() - 1;
            // -1 to ensure that when a diff is applied it's still positive

            var keys = typePointers.Select(p => (int) (p - _typePointerDiff)).ToArray();
            var values =
                structTypes.Select(type => new MessageMetadata(messageIdGetter(type), (short) counter.GetSize(type),
                    (short) (int) Marshal.OffsetOf(type, Envelope.FieldName))).ToArray();

            _metadata = new IntLookup<MessageMetadata>(keys, values);
        }

        protected MessageMetadata GetMessageMetadata(RuntimeTypeHandle handle)
        {
            var key = (int) (handle.Value.ToInt64() - _typePointerDiff);
            MessageMetadata value;
            _metadata.TryGet(key, out value);
            return value;
        }

        public static IMessageWriter Build(IStructSizeCounter counter, Func<Type, int> messageIdGetter,
            Type[] structTypes, ModuleBuilder module)
        {
            var writer = module.DefineType("MessageWriter", typeof (SomeWriter).Attributes, typeof (BaseMessageWriter));
            writer.AddInterfaceImplementation(typeof (IMessageWriter));
            var mi = typeof (SomeWriter).GetMethod("Write");

            var parameterTypes = mi.GetParameters().Select(pi => pi.ParameterType).ToArray();
            var requiredCustomModifiers = mi.GetParameters().Select(pi => pi.GetRequiredCustomModifiers()).ToArray();
            var optionalCustomModifiers = mi.GetParameters().Select(pi => pi.GetOptionalCustomModifiers()).ToArray();

            var method = writer.DefineMethod("Write", mi.Attributes, mi.CallingConvention, mi.ReturnType, null, null,
                parameterTypes,
                requiredCustomModifiers, optionalCustomModifiers);

            var genericParameters = method.DefineGenericParameters("TMessage");
            genericParameters[0].SetGenericParameterAttributes(GenericParameterAttributes.None);

            EmitBody(method);

            // initialize
            var instance = Activator.CreateInstance(writer.CreateType());
            ((BaseMessageWriter) instance).Init(counter, messageIdGetter, structTypes);
            return (IMessageWriter) instance;
        }

        private static void EmitBody(MethodBuilder method)
        {
            var byteChunkCtor = typeof (ByteChunk).GetConstructors().Single(ci => ci.GetParameters().Length == 2);

            var il = method.GetILGenerator();
            il.DeclareLocal(typeof (MessageMetadata));

            // load meta dictionary
            il.Emit(OpCodes.Ldarg_0);

            var getMessageMetadata = typeof (BaseMessageWriter).GetMethod("GetMessageMetadata",
                BindingFlags.NonPublic | BindingFlags.Instance);
            il.Emit(OpCodes.Ldtoken, method.GetGenericArguments()[0]);
            il.EmitCall(OpCodes.Call, getMessageMetadata, null);
            il.Emit(OpCodes.Stloc_0);

            // writer first
            il.Emit(OpCodes.Ldarg_3);

            // message id
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldfld, typeof (MessageMetadata).GetField("Id"));

            // store envelope in the field of a message
            il.Emit(OpCodes.Ldarg_2); // YES, you can load the managed reference and pass it as a pointer :D
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldfld, typeof (MessageMetadata).GetField("EnvelopeOffset"));
            il.Emit(OpCodes.Add);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldind_I8);

            il.Emit(OpCodes.Stind_I8);

            // message ByteChunk
            il.Emit(OpCodes.Ldarg_2); // YES, you can load the managed reference and pass it as a pointer :D
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldfld, typeof (MessageMetadata).GetField("Size"));
            il.Emit(OpCodes.Newobj, byteChunkCtor);

            //// call writer
            var writeMethod = typeof (WriteDelegate).GetMethod("Invoke");
            il.EmitCall(OpCodes.Callvirt, writeMethod, null);
            il.Emit(OpCodes.Ret);
        }
    }

    internal sealed class SomeWriter : BaseMessageWriter, IMessageWriter
    {
        public bool Write<TMessage>(ref Envelope envelope, ref TMessage message, WriteDelegate write)
            where TMessage : struct
        {
            throw new NotImplementedException();
        }
    }
}