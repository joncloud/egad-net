using BenchmarkDotNet.Attributes;

namespace Egad.BenchmarkTests
{
    [MemoryDiagnoser]
    public class ComplexTests : DataSetTests
    {
        public ComplexTests() : base(DataSetType.Parent | DataSetType.Child | DataSetType.DataTypes) { }
    }
}
