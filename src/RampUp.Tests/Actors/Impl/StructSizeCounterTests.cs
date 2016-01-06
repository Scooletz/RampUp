using System;
using NUnit.Framework;
using RampUp.Actors.Impl;

namespace RampUp.Tests.Actors.Impl
{
    public class StructSizeCounterTests
    {
        private readonly StructSizeCounter _counter;

        public StructSizeCounterTests()
        {
            _counter = new StructSizeCounter();
        }

        public struct SimpleBlittable
        {
            public int Value;
            public Guid G;
            public byte SomeByte;
        }

        public struct WithNullables
        {
            public int? Value;
            public Guid? G;
            public byte? SomeByte;
        }

        public struct FixedSizedBuffers
        {
            public unsafe fixed byte Bytes[1024];
        }

        public struct WithPointer
        {
            public unsafe WithPointer* Pointer;
        }

        public struct All
        {
            public SimpleBlittable Field1;
            public WithNullables Fields2;
            public FixedSizedBuffers Field3;
            public WithPointer Field4;
        }

        public struct SomeInterface
        {
            public IDisposable Disposable;
        }

        [TestCase(typeof(SimpleBlittable))]
        [TestCase(typeof(WithNullables))]
        [TestCase(typeof(FixedSizedBuffers))]
        [TestCase(typeof(WithPointer))]
        [TestCase(typeof(All))]
        [TestCase(typeof(string), ExpectedException = typeof(ArgumentException))]
        [TestCase(typeof(SomeInterface), ExpectedException = typeof(ArgumentException))]
        public void Size(Type t)
        {
            Console.WriteLine(_counter.GetSize(t));
        }
    }
}