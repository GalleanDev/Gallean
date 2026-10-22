using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed class MosrGatedCnnBlock : Module<torch.Tensor, torch.Tensor>
{
    private const double ShortcutOffset = 0.5;

    private readonly MosrLayerNorm norm;
    private readonly Conv2d fc1;
    private readonly Conv2d conv;
    private readonly Conv2d fc2;
    private readonly long gateChannels;
    private readonly long passThroughChannels;
    private readonly long convChannels;

    public MosrGatedCnnBlock(int dimension, double expansionRatio, double convRatio, int kernelSize)
        : base(nameof(MosrGatedCnnBlock))
    {
        int hidden = (int)(expansionRatio * dimension);
        int convolved = (int)(convRatio * dimension);

        gateChannels = hidden;
        passThroughChannels = hidden - convolved;
        convChannels = convolved;

        norm = new MosrLayerNorm(dimension);
        fc1 = Conv2d(dimension, hidden * 2, kernel_size: 3, stride: 1, padding: 1, bias: true);

        conv = Conv2d(
            convolved,
            convolved,
            kernel_size: kernelSize,
            stride: 1,
            padding: kernelSize / 2,
            groups: convolved,
            bias: true);

        fc2 = Conv2d(hidden, dimension, kernel_size: 3, stride: 1, padding: 1, bias: true);

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor normalized = norm.call(input);
        using torch.Tensor projected = fc1.call(normalized);
        using torch.Tensor gate = projected.narrow(1, 0, gateChannels);
        using torch.Tensor passThrough = projected.narrow(1, gateChannels, passThroughChannels);
        using torch.Tensor convolvedInput = projected.narrow(1, gateChannels + passThroughChannels, convChannels);
        using torch.Tensor convolved = conv.call(convolvedInput);
        using torch.Tensor combined = torch.cat([passThrough, convolved], dim: 1);
        using torch.Tensor activatedGate = functional.mish(gate);
        using torch.Tensor gated = activatedGate * combined;
        using torch.Tensor projectedBack = fc2.call(gated);
        using torch.Tensor activated = functional.mish(projectedBack);
        using torch.Tensor shortcut = input - ShortcutOffset;

        torch.Tensor result = activated + shortcut;

        return result.MoveToOuterDisposeScope();
    }
}
