using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed partial class BasicVsrSpyNet : Module<torch.Tensor, torch.Tensor, torch.Tensor>
{
    private const long ChunkSize = 2L * 1024 * 1024;
    private const int Alignment = 32;
    private const int Levels = 6;

    [ComponentName(Name = "basic_module")]
    private readonly ModuleList<Module<torch.Tensor, torch.Tensor>> basicModule;

    public BasicVsrSpyNet()
        : base(nameof(BasicVsrSpyNet))
    {
        basicModule = new ModuleList<Module<torch.Tensor, torch.Tensor>>();

        for (int level = 0; level < Levels; level++)
        {
            basicModule.Add(new BasicModule());
        }

        RegisterComponents();

        register_buffer("mean", torch.tensor([0.485f, 0.456f, 0.406f]).reshape(1, 3, 1, 1));
        register_buffer("std", torch.tensor([0.229f, 0.224f, 0.225f]).reshape(1, 3, 1, 1));
    }

    public override torch.Tensor forward(torch.Tensor reference, torch.Tensor supporting)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long height = reference.shape[2];
        long width = reference.shape[3];
        long alignedHeight = (height + Alignment - 1) / Alignment * Alignment;
        long alignedWidth = (width + Alignment - 1) / Alignment * Alignment;
        long total = reference.shape[0];

        torch.Tensor flow = TensorOperations.RunChunked(
            total,
            ChunkFrames(alignedHeight, alignedWidth),
            0,
            (start, count) => ComputeFlow(
                reference.narrow(0, start, count),
                supporting.narrow(0, start, count),
                alignedHeight,
                alignedWidth));

        return flow.MoveToOuterDisposeScope();
    }

    private torch.Tensor ComputeFlow(torch.Tensor reference, torch.Tensor supporting, long alignedHeight, long alignedWidth)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long height = reference.shape[2];
        long width = reference.shape[3];
        torch.Tensor alignedReference = Resize(reference, alignedHeight, alignedWidth);
        torch.Tensor alignedSupporting = Resize(supporting, alignedHeight, alignedWidth);
        torch.Tensor alignedFlow = Process(alignedReference, alignedSupporting);
        torch.Tensor flow = functional.interpolate(
            alignedFlow,
            size: [height, width],
            mode: torch.InterpolationMode.Bilinear,
            align_corners: false);

        flow.select(1, 0).mul_((double)width / alignedWidth);
        flow.select(1, 1).mul_((double)height / alignedHeight);

        return flow.MoveToOuterDisposeScope();
    }

    private static long ChunkFrames(long alignedHeight, long alignedWidth)
    {
        return Math.Max(1, ChunkSize / (alignedHeight * alignedWidth));
    }

    private static torch.Tensor Resize(torch.Tensor planes, long height, long width)
    {
        return functional.interpolate(
            planes, size: [height, width], mode: torch.InterpolationMode.Bilinear, align_corners: false);
    }

    private torch.Tensor Normalize(torch.Tensor planes)
    {
        return (planes - get_buffer("mean")!) / get_buffer("std")!;
    }

    private List<torch.Tensor> BuildPyramid(torch.Tensor planes)
    {
        List<torch.Tensor> pyramid = [Normalize(planes)];

        for (int level = 0; level < Levels - 1; level++)
        {
            pyramid.Insert(0, functional.avg_pool2d(
                pyramid[0], kernel_size: 2, stride: 2, count_include_pad: false));
        }

        return pyramid;
    }

    private torch.Tensor Process(torch.Tensor reference, torch.Tensor supporting)
    {
        List<torch.Tensor> references = BuildPyramid(reference);
        List<torch.Tensor> supportings = BuildPyramid(supporting);
        torch.Tensor flow = torch.zeros(
            [references[0].shape[0], 2, references[0].shape[2] / 2, references[0].shape[3] / 2],
            references[0].dtype);

        for (int level = 0; level < references.Count; level++)
        {
            torch.Tensor upsampledFlow = functional.interpolate(
                flow, scale_factor: [2.0, 2.0], mode: torch.InterpolationMode.Bilinear, align_corners: true).mul(2.0);

            upsampledFlow = PadToMatch(upsampledFlow, references[level]);

            torch.Tensor warpedSupporting = BasicVsrFlow.Warp(
                supportings[level], upsampledFlow.permute(0, 2, 3, 1), torch.GridSamplePaddingMode.Border);

            flow = basicModule[level].call(torch.cat([references[level], warpedSupporting, upsampledFlow], dim: 1)) + upsampledFlow;
        }

        return flow;
    }

    private static torch.Tensor PadToMatch(torch.Tensor flow, torch.Tensor reference)
    {
        torch.Tensor paddedFlow = flow;

        if (paddedFlow.shape[2] != reference.shape[2])
        {
            paddedFlow = functional.pad(paddedFlow, [0, 0, 0, 1], PaddingModes.Replicate);
        }

        if (paddedFlow.shape[3] != reference.shape[3])
        {
            paddedFlow = functional.pad(paddedFlow, [0, 1, 0, 0], PaddingModes.Replicate);
        }

        return paddedFlow;
    }
}
