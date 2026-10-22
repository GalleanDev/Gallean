using TorchSharp;

namespace Umbra.Tests;

public sealed class SurfaceCanvasTests
{
    private static UmbraFrame Frame(int width, int height)
    {
        UmbraSpriteMetadata metadata = new(width, height, width, height, 0, 0, false);

        return new UmbraFrame(TestHelpers.MakeSpriteHash("hash"), new UmbraSprite(metadata, new byte[width * height * 4]));
    }

    [Test]
    public void BackgroundTest()
    {
        SurfaceCanvas canvas = SurfaceCanvas.Build([Frame(2, 1)]);
        UmbraSpriteMetadata metadata = new(2, 1, 2, 1, 0, 0, false);
        byte[] pixels = [255, 0, 0, 255, 0, 0, 0, 0];

        using torch.Tensor composed = canvas.Compose(0, new UmbraSprite(metadata, pixels), 128.0 / 255.0);

        Assert.That(composed.reshape(-1).data<float>().ToArray(), Is.EqualTo([
            1.0f, 128.0f / 255.0f,
            0.0f, 128.0f / 255.0f,
            0.0f, 128.0f / 255.0f
        ]).Within(1e-6f));
    }

    [Test]
    public void CoverageTest()
    {
        SurfaceCanvas canvas = SurfaceCanvas.Build([Frame(2, 1)]);
        UmbraSpriteMetadata metadata = new(2, 1, 2, 1, 0, 0, false);
        byte[] pixels = [255, 0, 0, 255, 0, 0, 0, 64];

        using torch.Tensor coverage = canvas.GetCoverage(0, new UmbraSprite(metadata, pixels));

        Assert.That(coverage.shape, Is.EqualTo(new long[] { 1, 1, 2 }));
        Assert.That(coverage.reshape(-1).data<float>().ToArray(), Is.EqualTo([1.0f, 64.0f / 255.0f]).Within(1e-6f));
    }
}
