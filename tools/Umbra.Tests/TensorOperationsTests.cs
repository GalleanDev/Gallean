using TorchSharp;
using static TorchSharp.torch.nn;

namespace Umbra.Tests;

public sealed class TensorOperationsTests
{
    private static torch.Tensor Blur(torch.Tensor input)
    {
        using torch.Tensor padded = functional.pad(input, [0, 0, 1, 1], PaddingModes.Replicate);

        return functional.avg_pool2d(padded, [3L, 1L], [1L, 1L]);
    }

    private static torch.Tensor Double(torch.Tensor input)
    {
        return functional.interpolate(input, scale_factor: [2.0, 2.0], mode: torch.InterpolationMode.Nearest);
    }

    [Test]
    public void RunChunkedBelowTest()
    {
        using torch.Tensor input = torch.arange(24, torch.ScalarType.Float32).reshape(1, 4, 6);
        using torch.Tensor applied = TensorOperations.RunChunked(input, 4, 1, chunk => chunk.mul(2.0));
        using torch.Tensor expected = input.mul(2.0);

        Assert.That(applied.shape, Is.EqualTo(new long[] { 1, 4, 6 }));
        Assert.That(TestHelpers.MaxDifference(applied, expected), Is.Zero);
    }

    [Test]
    public void RunChunkedMatchTest()
    {
        using torch.Tensor input = torch.arange(60, torch.ScalarType.Float32).reshape(1, 10, 6);
        using torch.Tensor applied = TensorOperations.RunChunked(input, 3, 1, chunk => chunk.mul(2.0).add(1.0));
        using torch.Tensor expected = input.mul(2.0).add(1.0);

        Assert.That(applied.shape, Is.EqualTo(new long[] { 1, 10, 6 }));
        Assert.That(TestHelpers.MaxDifference(applied, expected), Is.Zero);
    }

    [Test]
    public void RunChunkedPartialTest()
    {
        using torch.Tensor input = torch.arange(42, torch.ScalarType.Float32).reshape(1, 7, 6);
        using torch.Tensor applied = TensorOperations.RunChunked(input, 3, 1, chunk => chunk.neg());
        using torch.Tensor expected = input.neg();

        Assert.That(applied.shape, Is.EqualTo(new long[] { 1, 7, 6 }));
        Assert.That(TestHelpers.MaxDifference(applied, expected), Is.Zero);
    }

    [Test]
    public void RunChunkedDimensionTest()
    {
        using torch.Tensor input = torch.arange(60, torch.ScalarType.Float32).reshape(10, 2, 3);
        using torch.Tensor applied = TensorOperations.RunChunked(input, 4, 0, chunk => chunk.mul(3.0));
        using torch.Tensor expected = input.mul(3.0);

        Assert.That(applied.shape, Is.EqualTo(new long[] { 10, 2, 3 }));
        Assert.That(TestHelpers.MaxDifference(applied, expected), Is.Zero);
    }

    [Test]
    public void RunChunkedRangeTest()
    {
        using torch.Tensor input = torch.arange(30, torch.ScalarType.Float32).reshape(5, 2, 3);
        using torch.Tensor applied = TensorOperations.RunChunked(5, 2, 0, (start, count) =>
        {
            using torch.Tensor slice = input.narrow(0, start, count);

            return slice.add(1.0);
        });
        using torch.Tensor expected = input.add(1.0);

        Assert.That(applied.shape, Is.EqualTo(new long[] { 5, 2, 3 }));
        Assert.That(TestHelpers.MaxDifference(applied, expected), Is.Zero);
    }

    [Test]
    public void RunChunkedFitTest()
    {
        using torch.Tensor input = torch.arange(48, torch.ScalarType.Float32).reshape(1, 1, 12, 4);
        using torch.Tensor applied = TensorOperations.RunChunked(input, 12, 2, 1, 1, Blur);
        using torch.Tensor expected = Blur(input);

        Assert.That(TestHelpers.MaxDifference(applied, expected), Is.Zero);
    }

    [Test]
    public void RunChunkedMatchAcrossTest()
    {
        using torch.Tensor input = torch.arange(48, torch.ScalarType.Float32).reshape(1, 1, 12, 4);
        using torch.Tensor applied = TensorOperations.RunChunked(input, 4, 2, 1, 1, Blur);
        using torch.Tensor expected = Blur(input);

        Assert.That(applied.shape, Is.EqualTo(new long[] { 1, 1, 12, 4 }));
        Assert.That(TestHelpers.MaxDifference(applied, expected), Is.Zero);
    }

    [Test]
    public void RunChunkedFinalChunkTest()
    {
        using torch.Tensor input = torch.arange(40, torch.ScalarType.Float32).reshape(1, 1, 10, 4);
        using torch.Tensor applied = TensorOperations.RunChunked(input, 4, 2, 1, 1, Blur);
        using torch.Tensor expected = Blur(input);

        Assert.That(applied.shape, Is.EqualTo(new long[] { 1, 1, 10, 4 }));
        Assert.That(TestHelpers.MaxDifference(applied, expected), Is.Zero);
    }

    [Test]
    public void RunChunkedWithoutMarginTest()
    {
        using torch.Tensor input = torch.arange(48, torch.ScalarType.Float32).reshape(1, 1, 12, 4);
        using torch.Tensor applied = TensorOperations.RunChunked(input, 4, 2, 0, 1, Blur);
        using torch.Tensor expected = Blur(input);

        Assert.That(TestHelpers.MaxDifference(applied, expected), Is.GreaterThan(0.0f));
    }

    [Test]
    public void RunChunkedTransformTest()
    {
        using torch.Tensor input = torch.arange(48, torch.ScalarType.Float32).reshape(1, 1, 12, 4);
        using torch.Tensor applied = TensorOperations.RunChunked(input, 4, 2, 1, 2, Double);
        using torch.Tensor expected = Double(input);

        Assert.That(applied.shape, Is.EqualTo(new long[] { 1, 1, 24, 8 }));
        Assert.That(TestHelpers.MaxDifference(applied, expected), Is.Zero);
    }
}
