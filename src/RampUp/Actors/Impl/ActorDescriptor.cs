using System;
using System.Collections.Generic;
using System.Linq;

namespace RampUp.Actors.Impl
{
    /// <summary>
    /// Provides an actor description.
    /// </summary>
    public sealed class ActorDescriptor
    {
        /// <summary>
        /// Builds the description on the actor instance basis.
        /// </summary>
        /// <param name="actor"></param>
        public ActorDescriptor(IActor actor)
        {
            HandledMessageTypes = GetHandledMessageTypes(actor.GetType());
        }

        /// <summary>
        /// Creates an artificially composed descriptor.
        /// </summary>
        /// <param name="handledMessageTypes"></param>
        public ActorDescriptor(IEnumerable<Type> handledMessageTypes)
        {
            HandledMessageTypes = handledMessageTypes;
        }

        public IEnumerable<Type> HandledMessageTypes { get; }

        /// <summary>
        /// Gets all the message types handled by the given <paramref name="handlerType"/>
        /// </summary>
        private static IEnumerable<Type> GetHandledMessageTypes(Type handlerType)
        {
            return
                ActorRegistry.GetHandleMethods(handlerType)
                    .Select(m => m.GetParameters()[1].ParameterType.GetElementType());
        }
    }
}