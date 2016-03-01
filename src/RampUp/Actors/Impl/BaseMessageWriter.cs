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
        protected Dictionary<Type, MessageMetadata> Meta;

        protected struct MessageMetadata
        {
            public readonly int Id;
            public readonly int Size;
            public readonly int EnvelopeOffset;

            public MessageMetadata(int id, int size, int envelopeOffset)
            {
                Id = id;
                Size = size;
                EnvelopeOffset = envelopeOffset;
            }
        }

        protected void Init(IStructSizeCounter counter, Func<Type, int> messageIdGetter, Type[] structTypes)
        {
            Meta = structTypes.ToDictionary(type => type, type => new MessageMetadata(messageIdGetter(type), counter.GetSize(type), (int)Marshal.OffsetOf(type, Envelope.FieldName)));
        }

        public static IMessageWriter Build(IStructSizeCounter counter, Func<Type, int> messageIdGetter,
            Type[] structTypes, ModuleBuilder module)
        {
            var writer = module.DefineType("MessageWriter", typeof(SomeWriter).Attributes, typeof(BaseMessageWriter));
            writer.AddInterfaceImplementation(typeof(IMessageWriter));
            var mi = typeof(SomeWriter).GetMethod("Write");

            var parameterTypes = mi.GetParameters().Select(pi => pi.ParameterType).ToArray();
            var requiredCustomModifiers = mi.GetParameters().Select(pi => pi.GetRequiredCustomModifiers()).ToArray();
            var optionalCustomModifiers = mi.GetParameters().Select(pi => pi.GetOptionalCustomModifiers()).ToArray();

            var method = writer.DefineMethod("Write", mi.Attributes, mi.CallingConvention, mi.ReturnType, null, null, parameterTypes,
                requiredCustomModifiers, optionalCustomModifiers);

            var genericParameters = method.DefineGenericParameters("TMessage");
            genericParameters[0].SetGenericParameterAttributes(GenericParameterAttributes.None);

            EmitBody(method);

            // initialize
            var instance = Activator.CreateInstance(writer.CreateType());
            ((BaseMessageWriter)instance).Init(counter, messageIdGetter, structTypes);
            return (IMessageWriter)instance;
        }

        private static void EmitBody(MethodBuilder method)
        {
            var byteChunkCtor = typeof(ByteChunk).GetConstructors().Single(ci => ci.GetParameters().Length == 2);

            var il = method.GetILGenerator();
            il.DeclareLocal(typeof(MessageMetadata));

            // load meta dictionary
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, typeof(BaseMessageWriter).GetField("Meta", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));

            il.Emit(OpCodes.Ldtoken, method.GetGenericArguments()[0]); // should be: method.GetGenericArguments()[0]
            il.EmitCall(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"), null);
            il.EmitCall(OpCodes.Callvirt, typeof(Dictionary<Type, MessageMetadata>).GetProperty("Item").GetGetMethod(true), null);
            il.Emit(OpCodes.Stloc_0);

            // writer first
            il.Emit(OpCodes.Ldarg_3);

            // message id
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldfld, typeof(MessageMetadata).GetField("Id"));

            // store envelope in the field of a message
            il.Emit(OpCodes.Ldarg_2); // YES, you can load the managed reference and pass it as a pointer :D
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldfld, typeof(MessageMetadata).GetField("EnvelopeOffset"));
            il.Emit(OpCodes.Add);

            il.Emit(OpCodes.Ldarg_1);   
            il.Emit(OpCodes.Ldind_I8);  
            
            il.Emit(OpCodes.Stind_I8);

            // message ByteChunk
            il.Emit(OpCodes.Ldarg_2); // YES, you can load the managed reference and pass it as a pointer :D
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldfld, typeof(MessageMetadata).GetField("Size"));
            il.Emit(OpCodes.Newobj, byteChunkCtor);

            //// call writer
            var writeMethod = typeof(WriteDelegate).GetMethod("Invoke");
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