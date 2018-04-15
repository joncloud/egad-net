using BenchmarkDotNet.Attributes;

namespace Egad.BenchmarkTests
{
    [MemoryDiagnoser]
    public class DataTypeTests : DataSetTests
    {
        public DataTypeTests() : base(DataSetType.DataTypes) { }
    }
}
