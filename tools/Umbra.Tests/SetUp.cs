using TorchSharp;

namespace Umbra.Tests;

[SetUpFixture]
public sealed class SetUp
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        torch.set_num_threads(1);
    }
}
