using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;
using GELU = TorchSharp.Modules.GELU;

namespace Umbra.CorridorKey;

internal sealed class CorridorKeyMlp : Module<torch.Tensor, torch.Tensor>
{
    [ComponentName(Name = "fc1")]
    private readonly Linear hiddenProjection;

    [ComponentName(Name = "act")]
    private readonly GELU activation;

    [ComponentName(Name = "fc2")]
    private readonly Linear outputProjection;

    public CorridorKeyMlp(int dimension, int hiddenDimension)
        : base(nameof(CorridorKeyMlp))
    {
        hiddenProjection = Linear(dimension, hiddenDimension, hasBias: true);
        activation = GELU(approximate: GELU.Approximate.none);
        outputProjection = Linear(hiddenDimension, dimension, hasBias: true);

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor tokens)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor hidden = hiddenProjection.call(tokens);
        using torch.Tensor activations = activation.call(hidden);

        return outputProjection.call(activations).MoveToOuterDisposeScope();
    }
}
