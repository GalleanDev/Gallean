using TorchSharp;

namespace Umbra.Tests;

public sealed class WanResampleTests
{
    [Test]
    public void Downsample2dShapeTest()
    {
        using WanResample resample = new(8, WanResampleType.Downsample2d);
        using torch.Tensor input = torch.zeros([1, 8, 3, 16, 16]);

        using torch.Tensor result = resample.call(input);

        Assert.That(result.shape, Is.EqualTo(new long[] { 1, 8, 3, 8, 8 }));
    }

    [Test]
    public void Upsample2dShapeTest()
    {
        using WanResample resample = new(8, WanResampleType.Upsample2d);
        using torch.Tensor input = torch.zeros([1, 8, 3, 8, 8]);

        using torch.Tensor result = resample.call(input);

        Assert.That(result.shape, Is.EqualTo(new long[] { 1, 4, 3, 16, 16 }));
    }

    [Test]
    public void Downsample3dFirstChunkTest()
    {
        using WanResample resample = new(8, WanResampleType.Downsample3d);
        using WanVaeCache cache = new(4);
        using torch.Tensor input = torch.zeros([1, 8, 4, 16, 16]);

        using torch.Tensor result = resample.Forward(input, cache);

        Assert.That(result.shape, Is.EqualTo(new long[] { 1, 8, 4, 8, 8 }));
        Assert.That(cache.Position, Is.EqualTo(1));
    }

    [Test]
    public void Downsample3dLaterChunkTest()
    {
        using WanResample resample = new(8, WanResampleType.Downsample3d);
        using WanVaeCache cache = new(4);
        using torch.Tensor first = torch.zeros([1, 8, 4, 16, 16]);
        using torch.Tensor second = torch.zeros([1, 8, 4, 16, 16]);

        using torch.Tensor warmed = resample.Forward(first, cache);

        cache.Rewind();

        using torch.Tensor result = resample.Forward(second, cache);

        Assert.That(result.shape, Is.EqualTo(new long[] { 1, 8, 2, 8, 8 }));
    }

    [Test]
    public void Upsample3dLaterChunkTest()
    {
        using WanResample resample = new(8, WanResampleType.Upsample3d);
        using WanVaeCache cache = new(4);
        using torch.Tensor first = torch.zeros([1, 8, 2, 8, 8]);
        using torch.Tensor second = torch.zeros([1, 8, 2, 8, 8]);

        using torch.Tensor warmed = resample.Forward(first, cache);

        cache.Rewind();

        using torch.Tensor result = resample.Forward(second, cache);

        Assert.That(result.shape, Is.EqualTo(new long[] { 1, 4, 4, 16, 16 }));
    }
}
