using System.Runtime.InteropServices;
using Xunit;

namespace Egad.UnitTests
{
    public class PlatformFactAttribute : FactAttribute
    {
        public PlatformFactAttribute()
        {
            Skip = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? ""
                : "TODO Update to run on non-windows";
        }
    }
}
