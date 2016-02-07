//using System;
//using System.Collections.Generic;
//using RampUp.Actors.Impl;

//namespace RampUp.Actors.System
//{
//    public sealed class ActorSystem
//    {
//        private readonly Dictionary<Type, IFeature> _features = new Dictionary<Type, IFeature>();
//        private readonly List<IActor> _featureActors = new List<IActor>();
//        private readonly IStructSizeCounter _counter = new StructSizeCounter();

//        public ActorSystem Add<TActor>(TActor actor, Action<RegistrationContext<TActor>> register)
//            where TActor : IActor
//        {
//            var ctx = new RegistrationContext<TActor>(actor, this);
//            register(ctx);

//            if (ctx.Actors.Count > 0)
//            {
//            //    ctx.Actors.Add(actor)
//            //    new CompositeActor(ctx.Actors.ToArray(),_counter,)
//            }
//            //TODO: add all the actors to the same ring buffer, combine if needed, provide descriptor
//            return this;
//        }

//        private TFeature GetOrAdd<TFeature>()
//            where TFeature : IFeature, new()
//        {
//            IFeature feature;
//            if (_features.TryGetValue(typeof (TFeature), out feature) == false)
//            {
//                feature = new TFeature();
//                _featureActors.AddRange(feature.GetFeatureActors());
//            }

//            return (TFeature) feature;
//        }

//        public class RegistrationContext<TActor>
//            where TActor : IActor
//        {
//            public readonly TActor Actor;
//            internal readonly List<IActor> Actors = new List<IActor>();
//            private readonly ActorSystem _actorSystem;

//            internal RegistrationContext(TActor actor, ActorSystem actorSystem)
//            {
//                Actor = actor;
//                _actorSystem = actorSystem;
//                Actors.Add(actor);
//            }

//            public RegistrationContext<TActor> Use<TFeature>(Action<TActor, TFeature> configure)
//                where TFeature : IFeature, new()
//            {
//                var feature = _actorSystem.GetOrAdd<TFeature>();
//                Actors.AddRange(feature.GetCoexistingActors(Actor));
//                configure(Actor, feature);
//                return this;
//            }
//        }
//    }
//}

namespace RampUp.Actors
{
}