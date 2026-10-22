using System.Text;
using Gallean.Common;
using TorchSharp;

namespace Umbra.Tests;

public sealed class SurfaceDiffuserTests
{
    private static SurfaceDiffuser Build()
    {
        Dictionary<string, torch.Tensor> embeddings = new()
        {
            ["normal"] = torch.zeros([1, 4, 128]),
            ["negative"] = torch.zeros([1, 4, 128])
        };

        WanDit dit = new(
            dim: 64,
            inChannels: 32,
            outChannels: 16,
            feedForwardDim: 128,
            textDim: 128,
            frequencyDim: 64,
            heads: 2,
            layers: 1,
            patch: (1, 2, 2));

        WanVae vae = new(dim: 8, latentChannels: 16, [1, 2, 4, 4], residualBlocks: 1);

        return new SurfaceDiffuser(
            new SurfaceWeights(dit.state_dict(), vae.state_dict()),
            dit,
            vae,
            new SurfaceContext(embeddings),
            denoisingSteps: 2);
    }

    private static List<string> FormatMask(torch.Tensor mask)
    {
        using torch.Tensor dense = mask.contiguous();

        float[] values = dense.data<float>().ToArray();
        long frames = mask.shape[2];
        long height = mask.shape[3];
        long width = mask.shape[4];
        List<string> rows = [];

        for (long frame = 0; frame < frames; frame++)
        {
            JoinStringBuilder lines = new(" ");

            for (long row = 0; row < height; row++)
            {
                StringBuilder sb = new();

                for (long column = 0; column < width; column++)
                {
                    sb.Append(values[(((frame * height) + row) * width) + column] > 0.0f ? '1' : '0');
                }

                lines.Add(sb.ToString());
            }

            rows.Add(lines.ToString());
        }

        return rows;
    }

    [Test]
    public void RunTest()
    {
        using SurfaceDiffuser diffuser = Build();
        using torch.Tensor images = torch.zeros([5, 3, 32, 32]);
        using torch.Tensor coverage = torch.ones([5, 1, 32, 32]);
        SurfaceConditioning conditioning = diffuser.EncodeConditioning(images, coverage);

        using torch.Tensor result = diffuser.Run(conditioning, SurfaceChannelType.Normal);

        Assert.That(result.shape, Is.EqualTo(new long[] { 5, 3, 32, 32 }));
    }

    [Test]
    public void FrameCountTest()
    {
        using SurfaceDiffuser diffuser = Build();
        using torch.Tensor images = torch.zeros([6, 3, 32, 32]);
        using torch.Tensor coverage = torch.ones([6, 1, 32, 32]);
        SurfaceConditioning conditioning = diffuser.EncodeConditioning(images, coverage);

        using torch.Tensor result = diffuser.Run(conditioning, SurfaceChannelType.Normal);

        Assert.That(result.shape, Is.EqualTo(new long[] { 6, 3, 32, 32 }));
    }

    [Test]
    public void MissingEmbeddingTest()
    {
        using SurfaceContext context = new(new Dictionary<string, torch.Tensor> { ["negative"] = torch.zeros([1, 4, 128]) });

        InvalidDataException ex = Assert.Throws<InvalidDataException>(() => context.BuildPositive(SurfaceChannelType.Normal, torch.ScalarType.Float32, torch.CPU));

        Assert.That(ex.Message, Is.EqualTo("The prompt embedding 'normal' is missing."));
    }

    [Test]
    public void EncodeConditioningSimpleTest()
    {
        using SurfaceDiffuser diffuser = Build();
        using torch.Tensor images = torch.rand([5, 3, 32, 32]);
        using torch.Tensor coverage = torch.ones([5, 1, 32, 32]);

        SurfaceConditioning first = diffuser.EncodeConditioning(images, coverage);
        SurfaceConditioning second = diffuser.EncodeConditioning(images, coverage);

        Assert.That(first.Encoded.shape, Is.EqualTo(second.Encoded.shape));
        Assert.That(TestHelpers.MaxDifference(first.Encoded, second.Encoded), Is.LessThan(1e-4f));
    }

    [Test]
    public void EncodeConditioningShapeTest()
    {
        using SurfaceDiffuser diffuser = Build();
        using torch.Tensor images = torch.zeros([5, 3, 32, 32]);
        using torch.Tensor coverage = torch.ones([5, 1, 32, 32]);

        SurfaceConditioning conditioning = diffuser.EncodeConditioning(images, coverage);

        Assert.That(conditioning.FrameCount, Is.EqualTo(5));
        Assert.That(conditioning.Encoded.shape, Is.EqualTo(new long[] { 1, 16, 4, 4, 4 }));
    }

    [Test]
    public void TotalFrameCountTest()
    {
        Assert.That(SurfaceDiffuser.GetTotalFrameCount(1), Is.EqualTo(3));
        Assert.That(SurfaceDiffuser.GetTotalFrameCount(44), Is.EqualTo(13));
        Assert.That(SurfaceDiffuser.GetTotalFrameCount(45), Is.EqualTo(14));
        Assert.That(SurfaceDiffuser.GetTotalFrameCount(67), Is.EqualTo(19));
    }

    [Test]
    public void MaskTest()
    {
        using SurfaceDiffuser diffuser = Build();
        using torch.Tensor images = torch.zeros([5, 3, 32, 32]);
        using torch.Tensor coverage = torch.zeros([5, 1, 32, 32]);

        coverage.select(0, 2).select(0, 0).select(0, 0).select(0, 0).fill_(1.0);

        SurfaceConditioning conditioning = diffuser.EncodeConditioning(images, coverage);

        Assert.That(FormatMask(conditioning.Mask), Is.EqualTo([
            "0000 0000 0000 0000",
            "0000 0000 0000 0000",
            "1110 1110 1110 0000",
            "0000 0000 0000 0000"
        ]));
    }

    [Test]
    public void EmptyCoverageTest()
    {
        using SurfaceDiffuser diffuser = Build();
        using torch.Tensor images = torch.zeros([5, 3, 32, 32]);
        using torch.Tensor coverage = torch.zeros([5, 1, 32, 32]);

        SurfaceConditioning conditioning = diffuser.EncodeConditioning(images, coverage);

        Assert.That(FormatMask(conditioning.Mask), Is.EqualTo([
            "1111 1111 1111 1111",
            "1111 1111 1111 1111",
            "1111 1111 1111 1111",
            "1111 1111 1111 1111"
        ]));
    }
}
