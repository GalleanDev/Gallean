using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.CorridorKey;

internal sealed class CorridorKeyProjection : Module<torch.Tensor, torch.Tensor>
{
    [ComponentName(Name = "proj")]
    private readonly Linear projection;

    public CorridorKeyProjection(int inputDimension, int outputDimension)
        : base(nameof(CorridorKeyProjection))
    {
        projection = Linear(inputDimension, outputDimension, hasBias: true);

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor planes)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor tokens = planes.flatten(2).transpose(1, 2);
        using torch.Tensor embeddings = projection.call(tokens);

        return embeddings
            .transpose(1, 2)
            .reshape(planes.shape[0], -1, planes.shape[2], planes.shape[3])
            .MoveToOuterDisposeScope();
    }
}
