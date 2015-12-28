using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using NUnit.Framework;

namespace RampUp.Tests
{
    [SetUpFixture]
    public class AugmentRampUpAssembly
    {
        //[SetUp]
        public void SetUp()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (assemblies.Any(a => a.GetName().Name == "RampUp"))
            {
                throw new InvalidOperationException("The assembly of RampUp has been already loaded");
            }

            var module = ModuleDefinition.ReadModule("RampUp.dll");

            Augment(module);

            using (var ms = new MemoryStream())
            {
                module.Write(ms);
                Assembly.Load(ms.ToArray());
            }

            assemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (assemblies.Any(a => a.GetName().Name == "RampUp") == false)
            {
                throw new InvalidOperationException("The assembly of RampUp hasn't been loaded after augmenting");
            }
        }

        private static void Augment(ModuleDefinition definition)
        {
            var atomicLong = definition.GetType("RampUp.Atomics", "AtomicLong");
            var atomicInt = definition.GetType("RampUp.Atomics", "AtomicInt");
        }
    }
}