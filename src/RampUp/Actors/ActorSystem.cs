using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using RampUp.Actors.Impl;
using RampUp.Buffers;
using RampUp.Ring;
using RampUp.Threading;

namespace RampUp.Actors
{
    public sealed class ActorSystem
    {
        private const int BatchSize = 100;
        private const int BufferSize = 1024*1024*128;
        private const int ThrowAfterNTrials = 3;

        private readonly Dictionary<Type, IFeature> _features = new Dictionary<Type, IFeature>();
        private readonly List<IActor> _featureActors = new List<IActor>();
        private readonly IStructSizeCounter _counter = new StructSizeCounter();
        private readonly List<Func<Runner>> _registrations = new List<Func<Runner>>();
        private readonly HashSet<Type> _messageTypes = new HashSet<Type>();
        private readonly Dictionary<Runner, Bus> _runnerBusMap = new Dictionary<Runner, Bus>();
        private long _messageTypePointerDiff;
        private IntLookup<int> _identifiers;
        private readonly List<ManyToOneRingBuffer> _buffers = new List<ManyToOneRingBuffer>();
        private RoundRobinThreadAffinedTaskScheduler _scheduler;
        private readonly ManualResetEventSlim _end = new ManualResetEventSlim();
        private CancellationTokenSource _source;
        private AggregateException _exception;

        public ActorSystem Add<TActor>(TActor actor, Action<RegistrationContext<TActor>> register)
            where TActor : IActor
        {
            var bus = new Bus();
            var ctx = new RegistrationContext<TActor>(actor, this, bus);
            Func<Runner> registration = () =>
            {
                register(ctx);
                ctx.Actors.Add(actor);
                var actors = ctx.Actors;
                return BuildRunner(actors, bus);
            };

            foreach (var messageType in new ActorDescriptor(actor).HandledMessageTypes)
            {
                _messageTypes.Add(messageType);
            }

            _registrations.Add(registration);
            return this;
        }

        private Runner BuildRunner(IEnumerable<IActor> actors, Bus bus)
        {
            var runner = new Runner(InstantiateBuffer(), _counter, GetMessageId, BatchSize, actors.ToArray());
            _runnerBusMap[runner] = bus;
            return runner;
        }

        private IRingBuffer InstantiateBuffer()
        {
            var buffer = new ManyToOneRingBuffer(new UnsafeBuffer(BufferSize + RingBufferDescriptor.TrailerLength));
            _buffers.Add(buffer);
            return buffer;
        }

        public void Start(Action<Exception> exceptionAction = null)
        {
            var module = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("RampUp_MessageWriter_" + Guid.NewGuid()),
                AssemblyBuilderAccess.Run).DefineDynamicModule("main");

            InitMessageTypesDictionary();
            var writer = MessageWriterBuilder.Build(_counter, GetMessageId, _messageTypes.ToArray(), module);

            var runners = _registrations.Select(f => f()).ToList();
            if (_featureActors.Count > 0)
            {
                runners.Add(BuildRunner(_featureActors, new Bus()));
            }

            var registry = CreateRegistry(runners);
            foreach (var kvp in _runnerBusMap)
            {
                var index = runners.FindIndex(r => ReferenceEquals(r, kvp.Key));
                kvp.Value.Init(GetId(index), registry, ThrowAfterNTrials, writer);
            }

            _source = new CancellationTokenSource();
            var token = _source.Token;
            _scheduler = new RoundRobinThreadAffinedTaskScheduler(runners.Count);
            var factory = new TaskFactory(_scheduler);

            var runningTasks = runners.Select(runner =>
            {
                return factory.StartNew(() =>
                {
                    try
                    {
                        while (token.IsCancellationRequested == false)
                        {
                            BatchInfo info;
                            runner.SpinOnce(out info);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception)
                    {
                        _source.Cancel();
                        throw;
                    }
                }, token);
            }).ToArray();

            // ReSharper disable once MethodSupportsCancellation
            Task.WhenAll(runningTasks).ContinueWith(t =>
            {
                _scheduler.Dispose();
                foreach (var buffer in _buffers)
                {
                    buffer.Dispose();
                }
                _exception = t.Exception;
                if (t.Exception != null)
                {
                    exceptionAction?.Invoke(t.Exception);
                }
                _end.Set();
            });
        }

        private static ActorRegistry CreateRegistry(IEnumerable<Runner> runners)
        {
            var tuples = runners.Select(
                (runner, index) => Tuple.Create(runner.Descriptor, runner.Buffer, GetId(index)))
                .ToArray();
            return new ActorRegistry(tuples);
        }

        private static ActorId GetId(int index)
        {
            return new ActorId((byte) (index + 1));
        }

        public void Stop()
        {
            _source.Cancel();
            _end.Wait();

            if (_exception != null)
            {
                throw _exception;
            }
        }

        private void InitMessageTypesDictionary()
        {
            var messageTypes = _messageTypes.ToArray();
            var messageTypePointers = messageTypes.Select(t => t.TypeHandle.Value.ToInt64()).ToArray();

            // -1 to ensure that when a diff is applied it's still positive
            _messageTypePointerDiff = messageTypePointers.Min() - 1;

            var keys = messageTypePointers.Select(p => (int) (p - _messageTypePointerDiff)).ToArray();
            var values = messageTypes.Select((type, index) => index + 1).ToArray();

            _identifiers = new IntLookup<int>(keys, values);
        }

        private int GetMessageId(Type messageType)
        {
            var key = (int) (messageType.TypeHandle.Value.ToInt64() - _messageTypePointerDiff);
            int value;
            if (_identifiers.TryGet(key, out value) == false)
            {
                throw new KeyNotFoundException($"{messageType} has been not found as a registered message type.");
            }

            return value;
        }

        private TFeature GetOrAdd<TFeature>()
            where TFeature : IFeature, new()
        {
            IFeature feature;
            if (_features.TryGetValue(typeof (TFeature), out feature) == false)
            {
                feature = new TFeature();
                _featureActors.AddRange(feature.GetFeatureActors());
            }

            return (TFeature) feature;
        }

        public class RegistrationContext<TActor>
            where TActor : IActor
        {
            public readonly TActor Actor;
            public readonly IBus Bus;
            internal readonly List<IActor> Actors = new List<IActor>();
            private readonly ActorSystem _actorSystem;

            internal RegistrationContext(TActor actor, ActorSystem actorSystem, IBus bus)
            {
                Actor = actor;
                _actorSystem = actorSystem;
                Bus = bus;
                Actors.Add(actor);
            }

            public RegistrationContext<TActor> Use<TFeature>(Action<TActor, TFeature> configure)
                where TFeature : IFeature, new()
            {
                var feature = _actorSystem.GetOrAdd<TFeature>();
                Actors.AddRange(feature.GetCoexistingActors(Actor));
                configure(Actor, feature);
                return this;
            }
        }
    }
}