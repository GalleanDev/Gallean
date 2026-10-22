using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.Ltx;

internal sealed class LtxTimestepEmbedder : Module<torch.Tensor, torch.Tensor>
{
    private const int FrequencySize = 256;
    private const double MaxPeriod = 10000;

    [ComponentName(Name = "linear_1")]
    private readonly Linear firstLayer;

    [ComponentName(Name = "linear_2")]
    private readonly Linear secondLayer;

    public LtxTimestepEmbedder(int hiddenSize)
        : base(nameof(LtxTimestepEmbedder))
    {
        firstLayer = Linear(FrequencySize, hiddenSize, hasBias: true);
        secondLayer = Linear(hiddenSize, hiddenSize, hasBias: true);

        RegisterComponents();
    }

    public static torch.Tensor GetTimestepEmbedding(torch.Tensor timestep)
    {
        using IDisposable scope = torch.NewDisposeScope();

        int half = FrequencySize / 2;

        using torch.Tensor steps = torch.arange(half, torch.ScalarType.Float32, timestep.device);
        using torch.Tensor exponents = steps.mul(-Math.Log(MaxPeriod) / half);
        using torch.Tensor frequencies = exponents.exp();
        using torch.Tensor floatTimestep = timestep.unsqueeze(-1).to(torch.ScalarType.Float32);
        using torch.Tensor arguments = floatTimestep.mul(frequencies.unsqueeze(0));
        using torch.Tensor cosine = arguments.cos();
        using torch.Tensor sine = arguments.sin();

        return torch.cat([cosine, sine], dim: -1).MoveToOuterDisposeScope();
    }

    public override torch.Tensor forward(torch.Tensor projected)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor hidden = firstLayer.forward(projected);
        using torch.Tensor activatedHidden = functional.silu(hidden);

        return secondLayer.forward(activatedHidden).MoveToOuterDisposeScope();
    }
}
