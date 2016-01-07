using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using RampUp.Buffers;
using RampUp.Ring;

namespace RampUp.Actors.Impl
{
    public sealed class MessageWriter : IMessageWriter
    {
        private readonly Dictionary<Type, Delegate> _sendDelegate = new Dictionary<Type, Delegate>();

        public MessageWriter(IStructSizeCounter counter, Type[] structs, Func<Type, int> messageIdGetter)
        {
            var envelopeSize = counter.GetSize(typeof (Envelope));
            foreach (var @struct in structs)
            {
                if (@struct.IsValueType == false)
                {
                    throw new ArgumentException(
                        $"The message type {@struct} isn't a value type. Only value types can be used as messages");
                }

                var structSize = counter.GetSize(@struct);
                _sendDelegate[@struct] = EmitSend(@struct, structSize, envelopeSize, messageIdGetter);
            }
        }

        private static Delegate EmitSend(Type @struct, int structSize, int envelopeSize, Func<Type, int> messageIdGetter)
        {
            var byteChunkCtor = typeof (ByteChunk).GetConstructors().Single(ci => ci.GetParameters().Length == 2);
            var name = "SendMessageOfType" + @struct.Namespace.Replace(".", "_") + "_" + @struct.Name;
            var dm = new DynamicMethod(name, typeof (bool),
                new[] {typeof (Envelope).MakeByRefType(), @struct.MakeByRefType(), typeof (IRingBuffer)},
                @struct.Assembly.Modules.First(), true);

            dm.DefineParameter(1, ParameterAttributes.Out | ParameterAttributes.In, "envelope");
            dm.DefineParameter(2, ParameterAttributes.Out | ParameterAttributes.In, "message");
            dm.DefineParameter(3, ParameterAttributes.In, "receiver");

            var il = dm.GetILGenerator();

            // buffer first
            il.Emit(OpCodes.Ldarg_2);

            //// message id
            il.Emit(OpCodes.Ldc_I4, messageIdGetter(@struct));

            //// envelope ByteChunk
            il.Emit(OpCodes.Ldarga_S, 0); // YES, you can load the managed reference and pass it as a pointer :D
            il.Emit(OpCodes.Conv_U);
            il.Emit(OpCodes.Ldc_I4, envelopeSize);
            il.Emit(OpCodes.Newobj, byteChunkCtor);

            //// message ByteChunk
            il.Emit(OpCodes.Ldarga_S, 2); // YES, you can load the managed reference and pass it as a pointer :D
            il.Emit(OpCodes.Conv_U);
            il.Emit(OpCodes.Ldc_I4, structSize);
            il.Emit(OpCodes.Newobj, byteChunkCtor);

            //// call write
            var writeMethod = typeof (IRingBuffer).GetMethod("Write");
            il.EmitCall(OpCodes.Callvirt, writeMethod, null);
            il.Emit(OpCodes.Ret);

            return dm.CreateDelegate(typeof (SendMessageDelegate<>).MakeGenericType(@struct));
        }

        public bool Write<TMessage>(ref Envelope envelope, ref TMessage message, IRingBuffer receiver)
            where TMessage : struct
        {
            var @delegate = _sendDelegate[typeof (TMessage)];
            return ((SendMessageDelegate<TMessage>) @delegate)(ref envelope, ref message, receiver);
        }

        private delegate bool SendMessageDelegate<TMessage>(
            ref Envelope envelope, ref TMessage message, IRingBuffer receiver);
    }
}