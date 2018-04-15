using BenchmarkDotNet.Attributes;

namespace Egad.BenchmarkTests
{
    [MemoryDiagnoser]
    public class JoinTests : DataSetTests
    {
        public JoinTests() : base(DataSetType.Parent | DataSetType.Child)
        {
        }
    }
}
