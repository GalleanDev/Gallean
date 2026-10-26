using System.Runtime.InteropServices;
using TorchSharp;

namespace Umbra.Tests;

[SetUpFixture]
public sealed class SetUp
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        if (OperatingSystem.IsMacOS() && RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            Assert.Ignore("TorchSharp no longer supports Intel macOS.");
        }

        torch.set_num_threads(1);
    }
}
