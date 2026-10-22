using TorchSharp;

namespace Umbra;

internal sealed record WanRope(torch.Tensor Frames, torch.Tensor Height, torch.Tensor Width) : IDisposable
{
    private const double Theta = 10000;

    public static WanRope Precompute(int dim, int end = 1024)
    {
        int spatial = dim / 3;
        int temporal = dim - (2 * spatial);

        return new WanRope(Axis(temporal, end), Axis(spatial, end), Axis(spatial, end));
    }

    public torch.Tensor Build(long frames, long height, long width, torch.Device device, long frameOffset = 0)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor frameFrequencies = Frames.narrow(0, frameOffset, frames).to(device)
            .reshape(frames, 1, 1, -1)
            .expand(frames, height, width, -1);
        using torch.Tensor heightFrequencies = Height.narrow(0, 0, height).to(device)
            .reshape(1, height, 1, -1)
            .expand(frames, height, width, -1);
        using torch.Tensor widthFrequencies = Width.narrow(0, 0, width).to(device)
            .reshape(1, 1, width, -1)
            .expand(frames, height, width, -1);

        torch.Tensor result = torch.cat([frameFrequencies, heightFrequencies, widthFrequencies], dim: -1)
            .reshape(frames * height * width, 1, -1);

        return result.MoveToOuterDisposeScope();
    }

    public void Dispose()
    {
        Frames.Dispose();
        Height.Dispose();
        Width.Dispose();
    }

    private static torch.Tensor Axis(int dim, int end)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor steps = torch.arange(0, dim, 2, torch.ScalarType.Float64, torch.CPU).narrow(0, 0, dim / 2);
        using torch.Tensor frequencies = steps.div(dim).mul(Math.Log(Theta)).neg().exp();
        using torch.Tensor positions = torch.arange(end, torch.ScalarType.Float64, torch.CPU);
        using torch.Tensor angles = torch.outer(positions, frequencies);
        using torch.Tensor magnitudes = torch.ones_like(angles);

        torch.Tensor result = torch.polar(magnitudes, angles);

        return result.MoveToOuterDisposeScope();
    }
}
