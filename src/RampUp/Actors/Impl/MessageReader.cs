using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using RampUp.Buffers;
using RampUp.Ring;

namespace RampUp.Actors.Impl
{
    /// <summary>
    /// Builds up a dynamic <see cref="MessageHandlerImpl"/> matching <see cref="MessageHandler"/> for a given actor.
    /// </summary>
    public sealed class MessageReader
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly IActor _handler;
        private readonly Action<MessageReader, int, ByteChunk> _reader;

        public MessageReader(IActor handler, IStructSizeCounter counter, Func<Type,int> messageIdGetter)
        {
            _handler = handler;
            var dm = BuildDispatchingMethod(handler, counter,messageIdGetter);

            _reader = (Action<MessageReader, int, ByteChunk>)dm.CreateDelegate(typeof(Action<MessageReader, int, ByteChunk>));
        }

        private static DynamicMethod BuildDispatchingMethod(IActor handler, IStructSizeCounter counter, Func<Type, int> messageIdGetter)
        {
            var handlerType = handler.GetType();
            var handleMethods =
                handlerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(mi =>
                    {
                        var isHandle = mi.Name == "Handle" && mi.ReturnType == typeof (void);
                        var parameterTypes = mi.GetParameters().Select(p => p.ParameterType).ToArray();
                        return isHandle &&
                               parameterTypes.Length == 2 &&
                               parameterTypes[0] == typeof (Envelope).MakeByRefType() &&
                               parameterTypes[1].IsByRef &&
                               parameterTypes[1].GetElementType().IsValueType;
                    })
                    .ToDictionary(m => messageIdGetter(m.GetParameters()[1].ParameterType.GetElementType()), m => m)
                    .OrderBy(kvp => kvp.Key)
                    .ToArray();

            var dm = new DynamicMethod("ReadMessagesFor_" + handlerType.Namespace.Replace(".", "_") + handlerType.Name,
                typeof (void), new[] {typeof (MessageReader), typeof (int), typeof (ByteChunk)},
                handlerType.Assembly.Modules.First(), true);

            var actorField = typeof (MessageReader).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(fi => fi.FieldType == typeof (IActor));

            var il = dm.GetILGenerator();

            // push handler
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, actorField);
            il.Emit(OpCodes.Castclass, handlerType);

            // push envelope
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldfld, typeof (ByteChunk).GetField("Pointer"));

            // push payload
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldfld, typeof(ByteChunk).GetField("Pointer"));
            il.Emit(OpCodes.Ldc_I4, counter.GetSize(typeof(Envelope)));
            il.Emit(OpCodes.Add);

            var endLbl = il.DefineLabel();

            // dispatch
            foreach (var method in handleMethods)
            {
                var lbl = il.DefineLabel();
                il.Emit(OpCodes.Ldarg_1); // messageTypeId
                il.Emit(OpCodes.Ldc_I4, method.Key);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brfalse_S, lbl);
                il.EmitCall(OpCodes.Callvirt, method.Value, null);
                il.Emit(OpCodes.Br_S, endLbl);
                il.MarkLabel(lbl);
            }

            // nothing was called, pop
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Pop);

            // end label
            il.MarkLabel(endLbl);
            il.Emit(OpCodes.Ret);
            return dm;
        }

        public void MessageHandlerImpl(int messageTypeId, ByteChunk chunk)
        {
            _reader(this, messageTypeId, chunk);
        }
    }
}