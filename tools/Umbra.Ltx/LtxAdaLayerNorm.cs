using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.Ltx;

internal sealed class LtxAdaLayerNorm : Module
{
    [ComponentName(Name = "emb")]
    private readonly LtxCombinedTimestepEmbedding embedding;

    [ComponentName(Name = "linear")]
    private readonly Linear linear;

    public LtxAdaLayerNorm(int hiddenSize, int modulationCount)
        : base(nameof(LtxAdaLayerNorm))
    {
        embedding = new LtxCombinedTimestepEmbedding(hiddenSize);
        linear = Linear(hiddenSize, modulationCount * hiddenSize, hasBias: true);

        RegisterComponents();
    }

    public (torch.Tensor Modulation, torch.Tensor Embedded) Forward(torch.Tensor timestep, torch.ScalarType dtype)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor embeddedTimestep = embedding.Forward(timestep, dtype);

        using torch.Tensor activatedTimestep = functional.silu(embeddedTimestep);

        torch.Tensor modulation = linear.forward(activatedTimestep);

        modulation.MoveToOuterDisposeScope();
        embeddedTimestep.MoveToOuterDisposeScope();

        return (modulation, embeddedTimestep);
    }
}
