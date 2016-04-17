using System;
using BenchmarkDotNet.Attributes;

namespace RampUp.Benchmarks
{
    public class MonomorphicTests
    {
        private Monomorphic _monomorphic;

        [Setup]
        public void SetUp()
        {
            _monomorphic = new Monomorphic(new Interface());
        }

        [Benchmark]
        public void _0Dispatch()
        {
            _monomorphic._0DispatchCall();
        }

        [Benchmark]
        public void _0Monomorphic()
        {
            _monomorphic._0MonomorphicCall();
        }

        [Benchmark]
        public void _1Dispatch()
        {
            _monomorphic._1DispatchCall();
        }

        [Benchmark]
        public void _1Monomorphic()
        {
            _monomorphic._1MonomorphicCall();
        }

        [Benchmark]
        public void _2Dispatch()
        {
            _monomorphic._2DispatchCall();
        }

        [Benchmark]
        public void _2Monomorphic()
        {
            _monomorphic._2MonomorphicCall();
        }
    }

    public sealed class Monomorphic
    {
        private readonly IInterface _i;
        private readonly Interface _interface;
        private readonly bool _isInterface;

        public Monomorphic(IInterface i)
        {
            _i = i;
            _interface = i as Interface;
            _isInterface = _interface != null;
        }

        public void _0MonomorphicCall()
        {
            if (_isInterface)
            {
                _interface.Do();
            }
            else
            {
                _i.Do();
            }
        }

        public void _0DispatchCall()
        {
            _i.Do();
        }

        public void _1DispatchCall()
        {
            _i.Do(1);
        }

        public void _1MonomorphicCall()
        {
            if (_isInterface)
            {
                _interface.Do(1);
            }
            else
            {
                _i.Do(1);
            }
        }

        public void _2DispatchCall()
        {
            _i.Do(1, 2);
        }

        public void _2MonomorphicCall()
        {
            if (_isInterface)
            {
                _interface.Do(1, 2);
            }
            else
            {
                _i.Do(1, 2);
            }
        }
    }

    public interface IInterface
    {
        void Do();
        void Do(int i);
        void Do(int i, int j);
    }

    public sealed class Interface : IInterface
    {
        public void Do()
        {
        }

        public void Do(int i)
        {
        }

        public void Do(int i, int j)
        {
        }
    }
}