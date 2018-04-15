using BenchmarkDotNet.Running;

namespace Egad.BenchmarkTests
{
    class Program
    {
        static void Main(string[] args) =>
            BenchmarkRunner.Run<Tests>();
    }
}
