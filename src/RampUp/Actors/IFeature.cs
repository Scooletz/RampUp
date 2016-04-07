using System.Collections.Generic;
using RampUp.Ring;

namespace RampUp.Actors
{
    /// <summary>
    /// Base class for features based on actors. Every feature provides two elements:
    /// - actors which are needed to coexist with an actor using this feature. They are obtained with <see cref="GetCoexistingActors"/>
    /// - actors which are needed to exist in the system (they are singletons, instansiated once)
    /// </summary>
    public interface IFeature
    {
        /// <summary>
        /// Gets actors coexisting with <paramref name="actor"/> to enable this feature. The actors will be run as a composite on the same <see cref="IRingBuffer"/>.
        /// </summary>
        /// <param name="actor">The actor</param>
        /// <returns></returns>
        IEnumerable<IActor> GetCoexistingActors(IActor actor);

        /// <summary>
        /// Gets actors required for this feature to work.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IActor> GetFeatureActors();
    }
}