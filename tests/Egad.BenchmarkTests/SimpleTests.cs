using BenchmarkDotNet.Attributes;

namespace Egad.BenchmarkTests
{
    [MemoryDiagnoser]
    public class SimpleTests : DataSetTests
    {
        public SimpleTests() : base(DataSetType.Parent) { }
    }
}
