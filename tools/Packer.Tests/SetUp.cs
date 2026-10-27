using System.Runtime.InteropServices;

namespace Packer.Tests;

[SetUpFixture]
public sealed class SetUp
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        if (OperatingSystem.IsMacOS() && RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            Assert.Ignore("Magick.NET does not support Intel macOS.");
        }
    }
}
