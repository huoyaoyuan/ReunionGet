using BenchmarkDotNet.Running;

namespace ReunionGet.Benchmarks
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            _ = BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .Run(args);
        }
    }
}
