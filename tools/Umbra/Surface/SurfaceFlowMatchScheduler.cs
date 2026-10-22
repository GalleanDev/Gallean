using TorchSharp;

namespace Umbra;

internal sealed class SurfaceFlowMatchScheduler(int steps)
{
    private const double Shift = 5;
    private const double TrainingTimesteps = 1000;

    private readonly double[] sigmas = BuildShiftedSigmas(steps);

    public int StepCount => sigmas.Length - 1;

    public double GetSigma(int step)
    {
        return sigmas[step];
    }

    public double GetTimestep(int step)
    {
        return sigmas[step] * TrainingTimesteps;
    }

    public torch.Tensor Step(torch.Tensor estimate, torch.Tensor sample, int step)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor floatSample = sample.to(torch.float32);
        torch.Tensor result = floatSample.add(estimate.to(torch.float32).mul(sigmas[step + 1] - sigmas[step])).to(sample.dtype);

        return result.MoveToOuterDisposeScope();
    }

    private static double[] BuildShiftedSigmas(int steps)
    {
        double[] schedule = new double[steps + 1];

        for (int step = 0; step < steps; step++)
        {
            double ramp = 1 - ((double)step / steps);

            schedule[step] = Shift * ramp / (1 + ((Shift - 1) * ramp));
        }

        schedule[steps] = 0;

        return schedule;
    }
}
