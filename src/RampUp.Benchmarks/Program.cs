using BenchmarkDotNet.Running;

using RampUp.Benchmarks.Actors.Impl;

namespace RampUp.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<BusBenchmarks>();
        }
    }
}