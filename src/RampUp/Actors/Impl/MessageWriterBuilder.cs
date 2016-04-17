using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using RampUp.Buffers;
using RampUp.Ring;

namespace RampUp.Actors.Impl
{
    public static class MessageWriterBuilder
    {
        private static readonly Type LookupType = typeof (LongLookup<MessageMetadata>);

        public static IMessageWriter Build(IStructSizeCounter counter, Func<Type, int> messageIdGetter,
            Type[] structTypes, ModuleBuilder module)
        {
            const string metadataFieldName = "metadata";
            var writer = module.DefineType("MessageWriter", typeof (SomeWriter).Attributes, typeof (object));
            writer.PadBefore();
            var metadataField = writer.DefineField(metadataFieldName, LookupType, FieldAttributes.Public);
            writer.PadAfter();
            writer.AddInterfaceImplementation(typeof (IMessageWriter));

            BuildWriteMethod(writer, metadataField);

            // initialize
            var metadata = MessageMetadata.BuildMetadata(counter, messageIdGetter, structTypes);

            var instance = Activator.CreateInstance(writer.CreateType());
            instance.GetType().GetField(metadataFieldName).SetValue(instance, metadata);
            return (IMessageWriter) instance;
        }

        public static void BuildWriteMethod(TypeBuilder writer, FieldBuilder metadataField)
        {
            var mi = typeof (SomeWriter).GetMethod("Write");
            var parameterTypes = mi.GetParameters().Select(pi => pi.ParameterType).ToArray();
            var requiredCustomModifiers = mi.GetParameters().Select(pi => pi.GetRequiredCustomModifiers()).ToArray();
            var optionalCustomModifiers = mi.GetParameters().Select(pi => pi.GetOptionalCustomModifiers()).ToArray();

            var method = writer.DefineMethod("Write", mi.Attributes, mi.CallingConvention, mi.ReturnType, null, null,
                parameterTypes,
                requiredCustomModifiers, optionalCustomModifiers);

            var genericParameters = method.DefineGenericParameters("TMessage");
            genericParameters[0].SetGenericParameterAttributes(GenericParameterAttributes.None);

            EmitBody(method, metadataField);
        }

        private static void EmitBody(MethodBuilder method, FieldInfo metadataField)
        {
            var byteChunkCtor = typeof (ByteChunk).GetConstructors().Single(ci => ci.GetParameters().Length == 2);

            var il = method.GetILGenerator();
            il.DeclareLocal(typeof (MessageMetadata));

            // load meta dictionary
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, metadataField);
            il.Emit(OpCodes.Ldtoken, method.GetGenericArguments()[0]);
            var getKey = typeof (MessageMetadata).GetMethod("GetKey", new[] {typeof (RuntimeTypeHandle)}, null);
            il.EmitCall(OpCodes.Call, getKey, null);
            var getOrDefault = LookupType.GetMethod("GetOrDefault");
            il.EmitCall(OpCodes.Call, getOrDefault, null);

            il.Emit(OpCodes.Stloc_0);

            // buffer first
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
            var writeMethod = typeof (IRingBuffer).GetMethod("Write");
            il.EmitCall(OpCodes.Callvirt, writeMethod, null);
            il.Emit(OpCodes.Ret);
        }
    }

    internal sealed class SomeWriter : IMessageWriter
    {
        public bool Write<TMessage>(ref Envelope envelope, ref TMessage message, IRingBuffer bufferToWrite)
            where TMessage : struct
        {
            throw new NotImplementedException();
        }
    }
}