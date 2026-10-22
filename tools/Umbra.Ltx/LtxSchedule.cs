using TorchSharp;

namespace Umbra.Ltx;

internal sealed class LtxSchedule(IReadOnlyList<double> sigmas)
{
    public static LtxSchedule Distilled { get; } = new([1.0, 0.99375, 0.9875, 0.98125, 0.975, 0.909375, 0.725, 0.421875, 0.0]);
    public static LtxSchedule Detailing { get; } = new([0.909375, 0.725, 0.421875, 0.0]);

    public int Steps => sigmas.Count - 1;

    public double GetSigma(int step)
    {
        return sigmas[step];
    }

    public torch.Tensor Denoise(torch.Tensor sample, torch.Tensor velocity, torch.Tensor timesteps)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor floatSample = sample.to(torch.ScalarType.Float32);
        using torch.Tensor floatVelocity = velocity.to(torch.ScalarType.Float32);
        using torch.Tensor scaledVelocity = floatVelocity.mul(timesteps.unsqueeze(-1));
        using torch.Tensor denoised = floatSample.sub(scaledVelocity);

        return denoised.to(sample.dtype).MoveToOuterDisposeScope();
    }

    public torch.Tensor Step(torch.Tensor sample, torch.Tensor denoised, int step)
    {
        using IDisposable scope = torch.NewDisposeScope();

        double sigma = sigmas[step];
        double delta = sigmas[step + 1] - sigma;

        using torch.Tensor floatSample = sample.to(torch.ScalarType.Float32);
        using torch.Tensor floatDenoised = denoised.to(torch.ScalarType.Float32);
        using torch.Tensor difference = floatSample.sub(floatDenoised);
        using torch.Tensor velocity = difference.div(sigma);
        using torch.Tensor displacement = velocity.mul(delta);
        using torch.Tensor next = floatSample.add(displacement);

        return next.to(sample.dtype).MoveToOuterDisposeScope();
    }
}
