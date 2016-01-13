using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RampUp.Ring;

namespace RampUp.Actors.Impl
{
    /// <summary>
    /// This class holds a registry of actors, aggregating them and precalculating different queries that are run against them.
    /// </summary>
    public sealed class ActorRegistry
    {
        private readonly IRingBuffer[][] _buffersPerMessageType;
        private readonly Dictionary<Type, int> _messageIdentifiers;
        private readonly IRingBuffer[] _buffers;

        public ActorRegistry(IReadOnlyCollection<Tuple<IActor, IRingBuffer, ActorId>> actors)
        {
            if (actors.Count > ActorId.MaxValue)
            {
                throw new ArgumentException("To many actors");
            }

            _messageIdentifiers = actors.Select(t => t.Item1)
                .Select(a => a.GetType())
                .SelectMany(GetHandledMessageTypes)
                .Distinct()
                .Select((t, i) => Tuple.Create(t, i + 1))
                .ToDictionary(t => t.Item1, t => t.Item2);

            if (_messageIdentifiers.Count == 0)
            {
                throw new ArgumentException("The handled messages set is empty. This is a useless registry");
            }

            var ringsGroupedByMessageId = actors.SelectMany(
                t =>
                    GetHandledMessageTypes(t.Item1.GetType())
                        .Select(messageType => new { MessageTypeId = _messageIdentifiers[messageType], Buffer = t.Item2 }))
                .GroupBy(a => a.MessageTypeId);

            _buffersPerMessageType = new IRingBuffer[_messageIdentifiers.Count + 1][];
            foreach (var rings in ringsGroupedByMessageId)
            {
                _buffersPerMessageType[rings.Key] = rings.Select(a => a.Buffer).ToArray();
            }

            var max = actors.Max(t => t.Item3.Value);
            _buffers = new IRingBuffer[max + 1];

            foreach (var actor in actors)
            {
                _buffers[actor.Item3.Value] = actor.Item2;
            }
        }

        public int GetMessageTypeId(Type messageType)
        {
            return _messageIdentifiers[messageType];
        }

        public IRingBuffer[] GetBuffers(Type messageType)
        {
            return _buffersPerMessageType[GetMessageTypeId(messageType)];
        }

        public IRingBuffer[] GetBuffers(int messageTypeId)
        {
            return _buffersPerMessageType[messageTypeId];
        }

        public IRingBuffer this[ActorId actor] => _buffers[actor.Value];

        /// <summary>
        /// Gets all the methods handling messages, declared in <paramref name="handlerType"/>.
        /// </summary>
        public static IEnumerable<MethodInfo> GetHandleMethods(Type handlerType)
        {
            return handlerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(mi =>
                {
                    var isHandle = mi.Name == "Handle" && mi.ReturnType == typeof(void);
                    var parameterTypes = mi.GetParameters().Select(p => p.ParameterType).ToArray();
                    return isHandle &&
                           parameterTypes.Length == 2 &&
                           parameterTypes[0] == typeof(Envelope).MakeByRefType() &&
                           parameterTypes[1].IsByRef &&
                           parameterTypes[1].GetElementType().IsValueType;
                });
        }

        /// <summary>
        /// Gets all the message types handled by the given <paramref name="handlerType"/>
        /// </summary>
        public static IEnumerable<Type> GetHandledMessageTypes(Type handlerType)
        {
            return GetHandleMethods(handlerType).Select(m => m.GetParameters()[1].ParameterType.GetElementType());
        }
    }
}