using TorchSharp;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.Ltx;

internal sealed class LtxCombinedTimestepEmbedding : Module
{
    [ComponentName(Name = "timestep_embedder")]
    private readonly LtxTimestepEmbedder embedder;

    public LtxCombinedTimestepEmbedding(int hiddenSize)
        : base(nameof(LtxCombinedTimestepEmbedding))
    {
        embedder = new LtxTimestepEmbedder(hiddenSize);

        RegisterComponents();
    }

    public torch.Tensor Forward(torch.Tensor timestep, torch.ScalarType dtype)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor timestepProjection = LtxTimestepEmbedder.GetTimestepEmbedding(timestep);
        using torch.Tensor hiddenProjection = timestepProjection.to(dtype);

        return embedder.forward(hiddenProjection).MoveToOuterDisposeScope();
    }
}
