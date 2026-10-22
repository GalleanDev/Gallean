using TorchSharp;

namespace Umbra.Ltx;

internal static class LtxRope
{
    private const long TableChunkTokens = 8192;

    public static (torch.Tensor Cos, torch.Tensor Sin) GetFrequencies(
        torch.Tensor positions,
        int hiddenSize,
        int heads,
        double theta,
        IReadOnlyList<int> maxPositions,
        torch.ScalarType dtype)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long tokens = positions.shape[2];
        long features = hiddenSize / 2 / heads;

        torch.Tensor cos = torch.empty([positions.shape[0], heads, tokens, features], dtype, positions.device);
        torch.Tensor sin = torch.empty([positions.shape[0], heads, tokens, features], dtype, positions.device);

        for (long start = 0; start < tokens; start += TableChunkTokens)
        {
            long count = Math.Min(TableChunkTokens, tokens - start);

            using IDisposable chunkScope = torch.NewDisposeScope();

            using torch.Tensor chunk = positions.narrow(2, start, count);

            (torch.Tensor chunkCos, torch.Tensor chunkSin) = BuildTable(chunk, hiddenSize, heads, theta, maxPositions, dtype);

            using torch.Tensor ownedCos = chunkCos;
            using torch.Tensor ownedSin = chunkSin;
            using torch.Tensor cosTarget = cos.narrow(2, start, count);
            using torch.Tensor sinTarget = sin.narrow(2, start, count);

            cosTarget.copy_(chunkCos);
            sinTarget.copy_(chunkSin);
        }

        cos.MoveToOuterDisposeScope();
        sin.MoveToOuterDisposeScope();

        return (cos, sin);
    }

    private static (torch.Tensor Cos, torch.Tensor Sin) BuildTable(
        torch.Tensor positions,
        int hiddenSize,
        int heads,
        double theta,
        IReadOnlyList<int> maxPositions,
        torch.ScalarType dtype)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long axes = positions.shape[1];
        long count = hiddenSize / (2 * axes);
        long padding = (hiddenSize / 2) - (count * axes);

        using torch.Tensor exponents = torch.linspace(0, 1, count, torch.ScalarType.Float64, positions.device);
        using torch.Tensor powers = torch.pow(theta, exponents);
        using torch.Tensor indices = powers.mul(Math.PI / 2.0).to(torch.ScalarType.Float32);

        float[] limits = new float[axes];

        for (int axis = 0; axis < axes; axis++)
        {
            limits[axis] = maxPositions[axis];
        }

        using torch.Tensor starts = positions.select(3, 0);
        using torch.Tensor ends = positions.select(3, 1);
        using torch.Tensor middle = starts.add(ends).div(2.0);
        using torch.Tensor limitTensor = torch.tensor(limits, torch.ScalarType.Float32, positions.device).view(1, axes, 1);
        using torch.Tensor fractionalPositions = middle.div(limitTensor).permute(0, 2, 1);
        using torch.Tensor centeredPositions = fractionalPositions.mul(2.0).sub(1.0);
        using torch.Tensor frequencies = centeredPositions.unsqueeze(-1).mul(indices).transpose(-1, -2).flatten(2);
        using torch.Tensor cosine = frequencies.cos();
        using torch.Tensor sine = frequencies.sin();
        using torch.Tensor cosinePadding = torch.ones([cosine.shape[0], cosine.shape[1], padding], cosine.dtype, cosine.device);
        using torch.Tensor sinePadding = torch.zeros([sine.shape[0], sine.shape[1], padding], sine.dtype, sine.device);
        using torch.Tensor paddedCosine = torch.cat([cosinePadding, cosine], dim: -1);
        using torch.Tensor paddedSine = torch.cat([sinePadding, sine], dim: -1);

        torch.Tensor cos = SplitHeads(paddedCosine, heads).to(dtype);
        torch.Tensor sin = SplitHeads(paddedSine, heads).to(dtype);

        cos.MoveToOuterDisposeScope();
        sin.MoveToOuterDisposeScope();

        return (cos, sin);
    }

    public static torch.Tensor Apply(torch.Tensor input, torch.Tensor cos, torch.Tensor sin, int heads)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long batch = input.shape[0];
        long tokens = input.shape[1];
        long headDimension = input.shape[2] / heads;
        long half = headDimension / 2;

        using torch.Tensor headedInput = input.view(batch, tokens, heads, headDimension).transpose(1, 2);
        using torch.Tensor firstHalf = headedInput.narrow(3, 0, half);
        using torch.Tensor secondHalf = headedInput.narrow(3, half, half);
        using torch.Tensor scaledFirstHalf = firstHalf.mul(cos);
        using torch.Tensor scaledSecondHalf = secondHalf.mul(cos);
        using torch.Tensor rotatedFirstHalf = scaledFirstHalf.sub(sin.mul(secondHalf));
        using torch.Tensor rotatedSecondHalf = scaledSecondHalf.add(sin.mul(firstHalf));
        using torch.Tensor output = torch.cat([rotatedFirstHalf, rotatedSecondHalf], dim: 3);

        return output.transpose(1, 2).reshape(batch, tokens, heads * headDimension).MoveToOuterDisposeScope();
    }

    private static torch.Tensor SplitHeads(torch.Tensor frequencies, int heads)
    {
        using IDisposable scope = torch.NewDisposeScope();

        return frequencies
            .reshape(frequencies.shape[0], frequencies.shape[1], heads, -1)
            .transpose(1, 2)
            .contiguous()
            .MoveToOuterDisposeScope();
    }
}
