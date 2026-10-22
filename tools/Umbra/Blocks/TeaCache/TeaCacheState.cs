using TorchSharp;

namespace Umbra;

internal sealed class TeaCacheState : IDisposable
{
    private torch.Tensor? previous;
    private torch.Tensor? residual;
    private double accumulated;

    public bool ShouldCompute(torch.Tensor modulation, TeaCacheOptions options, int forward)
    {
        bool retained = forward < options.RetainForwards || forward >= options.CutoffForwards;

        if (retained || previous is null || residual is null)
        {
            accumulated = 0;

            Remember(modulation);

            return true;
        }
        else
        {
            accumulated += Rescale(GetRelativeDistance(modulation, previous), options.Coefficients);

            Remember(modulation);

            if (accumulated < options.Threshold)
            {
                return false;
            }
            else
            {
                accumulated = 0;

                return true;
            }
        }
    }

    public torch.Tensor GetResidual()
    {
        if (residual is null)
        {
            throw new InvalidOperationException("The cache has no residual value.");
        }

        return residual;
    }

    public void SetResidual(torch.Tensor value)
    {
        residual?.Dispose();
        residual = value.DetachFromDisposeScope();
    }

    public void Reset()
    {
        previous?.Dispose();
        previous = null;
        residual?.Dispose();
        residual = null;
        accumulated = 0;
    }

    public void Dispose()
    {
        Reset();
    }

    private static double GetRelativeDistance(torch.Tensor modulation, torch.Tensor previous)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor difference = modulation.sub(previous).abs().mean();
        using torch.Tensor magnitude = previous.abs().mean();
        using torch.Tensor relativeDistance = difference.div(magnitude);

        return relativeDistance.to(torch.ScalarType.Float64).item<double>();
    }

    private static double Rescale(double value, IReadOnlyList<double> coefficients)
    {
        double result = 0;

        foreach (double coefficient in coefficients)
        {
            result = (result * value) + coefficient;
        }

        return result;
    }

    private void Remember(torch.Tensor modulation)
    {
        previous?.Dispose();
        previous = modulation.clone().DetachFromDisposeScope();
    }
}
