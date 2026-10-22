using TorchSharp;

namespace Umbra.Tests;

public sealed class WanVaeTilingTests
{
    [Test]
    public void TilesTest()
    {
        Assert.That(WanVaeTiling.BuildTiles(64, 32, 16), Is.EqualTo([
            (0L, 32L), (16L, 48L), (32L, 64L)
        ]));
    }

    [Test]
    public void TilesSingleTest()
    {
        Assert.That(WanVaeTiling.BuildTiles(24, 32, 16), Is.EqualTo([(0L, 24L)]));
    }

    [Test]
    public void LineMaskOverlapTest()
    {
        using torch.Tensor mask = WanVaeTiling.BuildLineMask(6, atStart: false, atEnd: true, border: 3, torch.ScalarType.Float32, torch.CPU);

        Assert.That(mask.data<float>().ToArray(), Is.EqualTo([
            1.0f / 3.0f, 2.0f / 3.0f, 1.0f, 1.0f, 1.0f, 1.0f
        ]).Within(1e-6f));
    }

    [Test]
    public void LineMaskTest()
    {
        using torch.Tensor mask = WanVaeTiling.BuildLineMask(4, atStart: true, atEnd: true, border: 2, torch.ScalarType.Float32, torch.CPU);

        Assert.That(mask.data<float>().ToArray(), Is.EqualTo([1.0f, 1.0f, 1.0f, 1.0f]).Within(1e-6f));
    }

    [Test]
    public void MaskTest()
    {
        using torch.Tensor mask = WanVaeTiling.BuildMask(
            4, 4, (false, true, true, true), (2, 2), torch.ScalarType.Float32, torch.CPU);

        Assert.That(mask.shape, Is.EqualTo(new long[] { 1, 1, 1, 4, 4 }));
        Assert.That(mask.reshape(-1).data<float>().ToArray(), Is.EqualTo([
            0.5f, 0.5f, 0.5f, 0.5f,
            1.0f, 1.0f, 1.0f, 1.0f,
            1.0f, 1.0f, 1.0f, 1.0f,
            1.0f, 1.0f, 1.0f, 1.0f
        ]).Within(1e-6f));
    }

    [Test]
    public void MaskDeviceTypeTest()
    {
        using torch.Tensor mask = WanVaeTiling.BuildMask(
            4, 4, (true, true, true, true), (2, 2), torch.ScalarType.Float32, torch.CPU);

        Assert.That(mask.device_type, Is.EqualTo(DeviceType.CPU));
    }

    [Test]
    public void EncodeTiledDtypeTest()
    {
        using WanVae vae = new(dim: 8, latentChannels: 16, [1, 2, 4, 4], residualBlocks: 1);
        using torch.Tensor images = torch.zeros([1, 3, 5, 64, 64], torch.ScalarType.Float64);

        using torch.Tensor full = vae.Encode(images);
        using torch.Tensor tiled = vae.EncodeTiled(images, (48, 48), (32, 32));

        Assert.That(full.dtype, Is.EqualTo(torch.ScalarType.Float32));
        Assert.That(tiled.dtype, Is.EqualTo(torch.ScalarType.Float32));
    }

    [Test]
    public void DecodeTiledDtypeTest()
    {
        using WanVae vae = new(dim: 8, latentChannels: 16, [1, 2, 4, 4], residualBlocks: 1);
        using torch.Tensor latents = torch.zeros([1, 16, 2, 8, 8], torch.ScalarType.Float64);

        using torch.Tensor full = vae.Decode(latents);
        using torch.Tensor tiled = vae.DecodeTiled(latents, (6, 6), (4, 4));

        Assert.That(full.dtype, Is.EqualTo(torch.ScalarType.Float32));
        Assert.That(tiled.dtype, Is.EqualTo(torch.ScalarType.Float32));
    }

    [Test]
    public void EncodeTiledTest()
    {
        using WanVae vae = new(dim: 8, latentChannels: 16, [1, 2, 4, 4], residualBlocks: 1);
        using torch.Tensor images = torch.zeros([1, 3, 5, 64, 64]);

        using torch.Tensor full = vae.Encode(images);
        using torch.Tensor tiled = vae.EncodeTiled(images, (64, 64), (32, 32));

        Assert.That(tiled.shape, Is.EqualTo(full.shape));
        Assert.That(TestHelpers.MaxDifference(tiled, full), Is.LessThan(1e-4f));
    }

    [Test]
    public void DecodeTiledTest()
    {
        using WanVae vae = new(dim: 8, latentChannels: 16, [1, 2, 4, 4], residualBlocks: 1);
        using torch.Tensor latents = torch.zeros([1, 16, 2, 8, 8]);

        using torch.Tensor full = vae.Decode(latents);
        using torch.Tensor tiled = vae.DecodeTiled(latents, (8, 8), (4, 4));

        Assert.That(tiled.shape, Is.EqualTo(full.shape));
        Assert.That(TestHelpers.MaxDifference(tiled, full), Is.LessThan(1e-4f));
    }

    [Test]
    public void EncodeTiledSplitTest()
    {
        using WanVae vae = new(dim: 8, latentChannels: 16, [1, 2, 4, 4], residualBlocks: 1);
        using torch.Tensor images = torch.rand([1, 3, 5, 64, 64]);

        using torch.Tensor full = vae.Encode(images);
        using torch.Tensor tiled = vae.EncodeTiled(images, (48, 48), (24, 24));

        Assert.That(WanVaeTiling.BuildTiles(64, 48, 24), Has.Count.EqualTo(2));
        Assert.That(tiled.shape, Is.EqualTo(full.shape));
    }
}
