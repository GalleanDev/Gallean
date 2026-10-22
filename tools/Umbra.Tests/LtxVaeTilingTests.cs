using TorchSharp;

namespace Umbra.Tests;

public sealed class LtxVaeTilingTests
{
    [Test]
    public void DecodeSimpleTest()
    {
        using torch.Tensor latent = torch.rand([1, 2, 3, 8, 8], torch.ScalarType.Float32);
        using torch.Tensor tiled = LtxVaeTiling.Apply(
            slice => slice.repeat_interleave(2, 3).repeat_interleave(2, 4), latent, 1, 2, 4, 2);
        using torch.Tensor untiled = LtxVaeTiling.Apply(
            slice => slice.repeat_interleave(2, 3).repeat_interleave(2, 4), latent, 1, 2, 32, 4);

        Assert.That(tiled.shape, Is.EqualTo(new long[] { 1, 2, 3, 16, 16 }));
        Assert.That(TestHelpers.MaxDifference(tiled, untiled), Is.LessThan(1e-6f));
    }

    [Test]
    public void EncodeSimpleTest()
    {
        using torch.Tensor video = torch.rand([1, 2, 3, 16, 16], torch.ScalarType.Float32);
        using torch.Tensor tiled = LtxVaeTiling.Apply(
            slice => torch.nn.functional.avg_pool3d(slice, [1, 2, 2], [1, 2, 2]), video, 2, 1, 4, 2);
        using torch.Tensor untiled = LtxVaeTiling.Apply(
            slice => torch.nn.functional.avg_pool3d(slice, [1, 2, 2], [1, 2, 2]), video, 2, 1, 32, 4);

        Assert.That(tiled.shape, Is.EqualTo(new long[] { 1, 2, 3, 8, 8 }));
        Assert.That(TestHelpers.MaxDifference(tiled, untiled), Is.LessThan(1e-6f));
    }
}
