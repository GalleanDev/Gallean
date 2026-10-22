using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.Ltx;

internal sealed class LtxVaeStatistics : Module, IParameterModule
{
    [ComponentName(Name = "mean-of-means")]
    private Parameter mean;

    [ComponentName(Name = "std-of-means")]
    private Parameter deviation;

    public LtxVaeStatistics(int latentChannels)
        : base(nameof(LtxVaeStatistics))
    {
        mean = new Parameter(torch.zeros(latentChannels), requires_grad: false);
        deviation = new Parameter(torch.ones(latentChannels), requires_grad: false);

        RegisterComponents();
    }

    public void SetParameter(string name, Parameter value)
    {
        switch (name)
        {
            case "mean-of-means":
                mean = this.ReplaceParameter(mean, name, value);
                break;

            case "std-of-means":
                deviation = this.ReplaceParameter(deviation, name, value);
                break;

            default:
                throw new NotSupportedException($"The parameter '{name}' of the '{GetName()}' module is not supported.");
        }
    }

    public torch.Tensor Normalize(torch.Tensor latent)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor meanOfMeans = Broadcast(mean, latent);
        using torch.Tensor scale = Broadcast(deviation, latent);
        using torch.Tensor centeredLatent = latent.sub(meanOfMeans);

        return centeredLatent.div(scale).MoveToOuterDisposeScope();
    }

    public torch.Tensor Unnormalize(torch.Tensor latent)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor meanOfMeans = Broadcast(mean, latent);
        using torch.Tensor scale = Broadcast(deviation, latent);
        using torch.Tensor scaledLatent = latent.mul(scale);

        return scaledLatent.add(meanOfMeans).MoveToOuterDisposeScope();
    }

    private static torch.Tensor Broadcast(torch.Tensor statistic, torch.Tensor latent)
    {
        using IDisposable scope = torch.NewDisposeScope();

        return statistic.view(1, -1, 1, 1, 1).to(latent.dtype, latent.device).MoveToOuterDisposeScope();
    }
}
