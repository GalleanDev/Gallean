using TorchSharp;

namespace Umbra.Tests;

public sealed class WanRmsNormTests
{
    [Test]
    public void UnitVectorTest()
    {
        using WanRmsNorm norm = new(4);
        using torch.Tensor input = torch.ones([1, 4, 1, 1]);

        using torch.Tensor result = norm.call(input);

        Assert.That(result.reshape(-1).data<float>().ToArray(), Is.EqualTo([1.0f, 1.0f, 1.0f, 1.0f]));
    }

    [Test]
    public void ScaleInvariantTest()
    {
        using WanRmsNorm norm = new(4);
        using torch.Tensor input = torch.ones([1, 4, 1, 1]).mul(7.0);

        using torch.Tensor result = norm.call(input);

        Assert.That(result.reshape(-1).data<float>().ToArray(), Is.EqualTo([1.0f, 1.0f, 1.0f, 1.0f]));
    }
}
