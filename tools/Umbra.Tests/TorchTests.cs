using TorchSharp;

namespace Umbra.Tests;

public sealed class TorchTests
{
    [Test]
    public void InterpolateTest()
    {
        using torch.Tensor source = torch.ones(1, 3, 2, 2);
        using torch.Tensor scaled = torch.nn.functional.interpolate(
            source, scale_factor: [2.0, 2.0], mode: torch.InterpolationMode.Bilinear, align_corners: false);

        Assert.That(scaled.shape, Is.EqualTo(new long[] { 1, 3, 4, 4 }));
        Assert.That(scaled.sum().ToSingle(), Is.EqualTo(48.0f));
    }
}
