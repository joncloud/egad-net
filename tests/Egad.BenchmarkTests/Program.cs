using BenchmarkDotNet.Running;
using System;
using System.Linq;

namespace Egad.BenchmarkTests
{
    class Program
    {
        static void Main(string[] args)
        {
            var typeName = args.DefaultIfEmpty("Join").ElementAtOrDefault(0);
            var fullTypeName = string.Join(
                ".",
                nameof(Egad),
                nameof(BenchmarkTests),
                typeName + "Tests"
            );
            BenchmarkRunner.Run(Type.GetType(fullTypeName));
        }
    }
}
