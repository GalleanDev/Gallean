using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed class BasicVsrSecondOrderDeformableAlignment
    : Module<torch.Tensor, torch.Tensor, torch.Tensor, torch.Tensor, torch.Tensor>
{
    private const int Dilation = 1;
    private const int MaxResidueMagnitude = 10;
    private const int Padding = 1;
    private const double NegativeSlope = 0.1;
    private const int OffsetChannelsPerGroup = 27;

    private readonly Parameter bias;

    [ComponentName(Name = "conv_offset")]
    private readonly ModuleChain convOffset;

    private readonly Parameter weight;

    public BasicVsrSecondOrderDeformableAlignment(int features, int deformableGroups)
        : base(nameof(BasicVsrSecondOrderDeformableAlignment))
    {
        weight = Parameter(torch.zeros(features, 2 * features, 3, 3));
        bias = Parameter(torch.zeros(features));

        convOffset = new ModuleChain(
        [
            Conv2d((3 * features) + 4, features, kernel_size: 3, stride: 1, padding: 1),
            LeakyReLU(NegativeSlope, inplace: true),
            Conv2d(features, features, kernel_size: 3, stride: 1, padding: 1),
            LeakyReLU(NegativeSlope, inplace: true),
            Conv2d(features, features, kernel_size: 3, stride: 1, padding: 1),
            LeakyReLU(NegativeSlope, inplace: true),
            Conv2d(features, OffsetChannelsPerGroup * deformableGroups, kernel_size: 3, stride: 1, padding: 1)
        ]);

        RegisterComponents();
    }

    public override torch.Tensor forward(
        torch.Tensor features,
        torch.Tensor condition,
        torch.Tensor firstFlow,
        torch.Tensor secondFlow)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor prediction = convOffset.call(torch.cat([condition, firstFlow, secondFlow], dim: 1));
        torch.Tensor[] parts = torch.chunk(prediction, 3, dim: 1);
        torch.Tensor residue = torch.cat([parts[0], parts[1]], dim: 1).tanh().mul(MaxResidueMagnitude);
        torch.Tensor[] residues = torch.chunk(residue, 2, dim: 1);

        torch.Tensor offset = torch.cat(
            [
                residues[0] + Tile(firstFlow, residues[0].shape[1] / 2),
                residues[1] + Tile(secondFlow, residues[1].shape[1] / 2)
            ],
            dim: 1);

        torch.Tensor result = BasicVsrDeformConv.Apply(features, offset, parts[2].sigmoid(), weight, bias, Padding, Dilation);

        return result.MoveToOuterDisposeScope();
    }

    private static torch.Tensor Tile(torch.Tensor flow, long count)
    {
        return flow.flip(1).repeat(1, count, 1, 1);
    }
}
