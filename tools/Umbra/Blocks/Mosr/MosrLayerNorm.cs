using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed class MosrLayerNorm : Module<torch.Tensor, torch.Tensor>
{
    private const double Epsilon = 1e-6;

    private readonly Parameter weight;
    private readonly Parameter bias;

    public MosrLayerNorm(int dimension)
        : base(nameof(MosrLayerNorm))
    {
        weight = new Parameter(torch.ones(dimension));
        bias = new Parameter(torch.zeros(dimension));

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor mean = input.mean([1], keepdim: true);
        using torch.Tensor centered = input - mean;
        using torch.Tensor variance = centered.pow(2).mean([1], keepdim: true);
        using torch.Tensor normalized = centered / torch.sqrt(variance + Epsilon);
        using torch.Tensor scaled = weight.unsqueeze(1).unsqueeze(2) * normalized;

        torch.Tensor result = scaled + bias.unsqueeze(1).unsqueeze(2);

        return result.MoveToOuterDisposeScope();
    }
}
