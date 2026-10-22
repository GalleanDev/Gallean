using Gallean.Common;
using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed class BasicVsrNetwork : Module
{
    private const int DeformableGroups = 16;
    private const int ExtractBlockCount = 5;
    private const int ReconstructBlockCount = 5;
    private const int TailChannels = 64;
    private const int UpsampleMargin = 4;
    private const int UpscaleFactor = 4;
    private const double NegativeSlope = 0.1;

    private static readonly string[] BranchNames = ["backward_1", "forward_1", "backward_2", "forward_2"];

    private readonly ModuleDict<Module<torch.Tensor, torch.Tensor>> backbone;

    [ComponentName(Name = "conv_hr")]
    private readonly Conv2d convHr;

    [ComponentName(Name = "conv_last")]
    private readonly Conv2d convLast;

    [ComponentName(Name = "deform_align")]
    private readonly ModuleDict<BasicVsrSecondOrderDeformableAlignment> deformAlign;

    [ComponentName(Name = "feat_extract")]
    private readonly BasicVsrConvResidualBlocks featureExtract;

    private readonly long chunkRows;
    private readonly int midChannels;
    private readonly string scratchDirectory;
    private readonly BasicVsrConvResidualBlocks reconstruction;
    private readonly BasicVsrSpyNet spynet;
    private readonly Conv2d upconv1;
    private readonly Conv2d upconv2;

    public BasicVsrNetwork(int midChannels, int blockCount, string scratchDirectory, long chunkRows)
        : base(nameof(BasicVsrNetwork))
    {
        this.chunkRows = chunkRows;
        this.midChannels = midChannels;
        this.scratchDirectory = scratchDirectory;

        spynet = new BasicVsrSpyNet();
        featureExtract = new BasicVsrConvResidualBlocks(3, midChannels, ExtractBlockCount);

        deformAlign = new ModuleDict<BasicVsrSecondOrderDeformableAlignment>();
        backbone = new ModuleDict<Module<torch.Tensor, torch.Tensor>>();

        for (int index = 0; index < BranchNames.Length; index++)
        {
            deformAlign.Add(BranchNames[index], new BasicVsrSecondOrderDeformableAlignment(midChannels, DeformableGroups));
            backbone.Add(BranchNames[index], new BasicVsrConvResidualBlocks((2 + index) * midChannels, midChannels, blockCount));
        }

        reconstruction = new BasicVsrConvResidualBlocks(5 * midChannels, midChannels, ReconstructBlockCount);
        upconv1 = Conv2d(midChannels, midChannels * 4, kernel_size: 3, stride: 1, padding: 1, bias: true);
        upconv2 = Conv2d(midChannels, TailChannels * 4, kernel_size: 3, stride: 1, padding: 1, bias: true);
        convHr = Conv2d(TailChannels, TailChannels, kernel_size: 3, stride: 1, padding: 1);
        convLast = Conv2d(TailChannels, 3, kernel_size: 3, stride: 1, padding: 1);

        RegisterComponents();
    }

    public void Upsample(torch.Tensor sequence, Action<int, torch.Tensor> frameFunc)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using CleanupList cleanupList = new();

        bool mirrored = IsMirrorExtended(sequence);
        FrameFile spatial = cleanupList.Add(ExtractSpatial(sequence));
        (torch.Tensor Forward, torch.Tensor Backward) flows = ComputeFlow(sequence, mirrored);
        Dictionary<string, FrameFile> branches = [];

        foreach (string branch in BranchNames)
        {
            FrameFile propagated = Propagate(spatial, branch, branches, SelectFlows(branch, flows));

            branches[branch] = cleanupList.Add(propagated);
        }

        for (int index = 0; index < spatial.Count; index++)
        {
            using IDisposable frameScope = torch.NewDisposeScope();

            frameFunc(index, UpsampleFrame(sequence, spatial, branches, index));
        }
    }

    private FrameFile CreateFrameFile(int frames, long batch, long height, long width, torch.ScalarType type)
    {
        Directory.CreateDirectory(scratchDirectory);

        string path = Path.Combine(scratchDirectory, Path.GetRandomFileName());

        return new FrameFile(frames, [batch, midChannels, height, width], type, path);
    }

    private static bool IsMirrorExtended(torch.Tensor sequence)
    {
        if (sequence.shape[1] % 2 == 0)
        {
            using IDisposable scope = torch.NewDisposeScope();

            torch.Tensor[] half = torch.chunk(sequence, 2, dim: 1);

            return (half[0] - half[1].flip(1)).norm().ToSingle() == 0;
        }
        else
        {
            return false;
        }
    }

    private static torch.Tensor SelectFlows(string branch, (torch.Tensor Forward, torch.Tensor Backward) flows)
    {
        if (branch.StartsWith("backward", StringComparison.Ordinal))
        {
            return flows.Backward;
        }
        else
        {
            return flows.Forward;
        }
    }

    private static List<int> BuildFrameOrder(int steps, bool backward)
    {
        List<int> order = [];

        for (int index = 0; index <= steps; index++)
        {
            order.Add(index);
        }

        if (backward)
        {
            order.Reverse();
        }

        return order;
    }

    private static List<int> BuildFlowOrder(int steps, bool backward, List<int> frameOrder)
    {
        if (backward)
        {
            return frameOrder;
        }
        else
        {
            List<int> order = [];

            for (int index = -1; index < steps; index++)
            {
                order.Add(index);
            }

            return order;
        }
    }

    private FrameFile ExtractSpatial(torch.Tensor sequence)
    {
        int frames = (int)sequence.shape[1];

        FrameFile frameFile = CreateFrameFile(frames, sequence.shape[0], sequence.shape[3], sequence.shape[4], sequence.dtype);

        for (int index = 0; index < frames; index++)
        {
            using IDisposable scope = torch.NewDisposeScope();

            torch.Tensor feature = featureExtract.call(sequence.select(1, index));

            frameFile.Write(index, feature);
        }

        return frameFile;
    }

    private (torch.Tensor Forward, torch.Tensor Backward) ComputeFlow(torch.Tensor sequence, bool mirrored)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long batch = sequence.shape[0];
        long frames = sequence.shape[1];
        long channels = sequence.shape[2];
        long height = sequence.shape[3];
        long width = sequence.shape[4];

        torch.Tensor earlierFrames = sequence.narrow(1, 0, frames - 1).reshape(-1, channels, height, width);
        torch.Tensor laterFrames = sequence.narrow(1, 1, frames - 1).reshape(-1, channels, height, width);
        torch.Tensor backwardFlows = spynet.call(earlierFrames, laterFrames).view(batch, frames - 1, 2, height, width);
        torch.Tensor forwardFlows;

        if (mirrored)
        {
            forwardFlows = backwardFlows.flip(1);
        }
        else
        {
            forwardFlows = spynet.call(laterFrames, earlierFrames).view(batch, frames - 1, 2, height, width);
        }

        forwardFlows.MoveToOuterDisposeScope();
        backwardFlows.MoveToOuterDisposeScope();

        return (forwardFlows, backwardFlows);
    }

    private FrameFile Propagate(
        FrameFile spatial,
        string branchName,
        Dictionary<string, FrameFile> branches,
        torch.Tensor flows)
    {
        bool backward = branchName.StartsWith("backward", StringComparison.Ordinal);
        int steps = (int)flows.shape[1];
        List<int> frameOrder = BuildFrameOrder(steps, backward);
        List<int> flowOrder = BuildFlowOrder(steps, backward, frameOrder);
        List<string> completed = [];

        foreach (string name in BranchNames)
        {
            if (name != branchName)
            {
                completed.Add(name);
            }
            else
            {
                break;
            }
        }

        FrameFile outputs = CreateFrameFile(frameOrder.Count, flows.shape[0], flows.shape[3], flows.shape[4], flows.dtype);
        torch.Tensor? firstOrder = null;
        torch.Tensor? secondOrder = null;

        for (int position = 0; position < frameOrder.Count; position++)
        {
            using IDisposable scope = torch.NewDisposeScope();

            torch.Tensor currentFeature = spatial.Read(frameOrder[position]);
            torch.Tensor propagated;

            if (firstOrder is not null)
            {
                torch.Tensor firstFlow = flows.select(1, flowOrder[position]);
                torch.Tensor firstCondition = BasicVsrFlow.Warp(firstOrder, firstFlow.permute(0, 2, 3, 1), torch.GridSamplePaddingMode.Zeros);
                torch.Tensor secondFeature;
                torch.Tensor secondFlow;
                torch.Tensor secondCondition;

                if (secondOrder is not null)
                {
                    secondFeature = secondOrder;
                    secondFlow = firstFlow + BasicVsrFlow.Warp(
                        flows.select(1, flowOrder[position - 1]),
                        firstFlow.permute(0, 2, 3, 1),
                        torch.GridSamplePaddingMode.Zeros);
                    secondCondition = BasicVsrFlow.Warp(
                        secondFeature, secondFlow.permute(0, 2, 3, 1), torch.GridSamplePaddingMode.Zeros);
                }
                else
                {
                    secondFeature = torch.zeros_like(firstOrder);
                    secondFlow = torch.zeros_like(firstFlow);
                    secondCondition = torch.zeros_like(firstCondition);
                }

                propagated = deformAlign[branchName].call(
                    torch.cat([firstOrder, secondFeature], dim: 1),
                    torch.cat([firstCondition, currentFeature, secondCondition], dim: 1),
                    firstFlow,
                    secondFlow);
            }
            else
            {
                propagated = torch.zeros([flows.shape[0], midChannels, flows.shape[3], flows.shape[4]], flows.dtype);
            }

            List<torch.Tensor> parts = [currentFeature];

            foreach (string name in completed)
            {
                FrameFile frameFile = branches[name];

                parts.Add(frameFile.Read(frameOrder[position]));
            }

            parts.Add(propagated);

            propagated += backbone[branchName].call(torch.cat(parts, dim: 1));

            outputs.Write(frameOrder[position], propagated);

            secondOrder?.Dispose();
            secondOrder = firstOrder;
            firstOrder = propagated.MoveToOuterDisposeScope();
        }

        firstOrder?.Dispose();
        secondOrder?.Dispose();

        return outputs;
    }

    private torch.Tensor UpsampleFrame(
        torch.Tensor sequence,
        FrameFile spatial,
        Dictionary<string, FrameFile> branches,
        int index)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor features = MergeFeatures(spatial, branches, index);
        using torch.Tensor reconstructedFeatures = reconstruction.call(features);
        using torch.Tensor residual = TensorOperations.RunChunked(
            reconstructedFeatures,
            chunkRows,
            2,
            UpsampleMargin,
            UpscaleFactor,
            Amplify);
        using torch.Tensor upsampledFrame = functional.interpolate(
            sequence.select(1, index),
            scale_factor: [UpscaleFactor, UpscaleFactor],
            mode: torch.InterpolationMode.Bilinear,
            align_corners: false);

        torch.Tensor result = residual + upsampledFrame;

        return result.MoveToOuterDisposeScope();
    }

    private static torch.Tensor MergeFeatures(FrameFile spatial, Dictionary<string, FrameFile> branches, int index)
    {
        using IDisposable scope = torch.NewDisposeScope();

        List<torch.Tensor> parts = [spatial.Read(index)];

        foreach (string name in BranchNames)
        {
            FrameFile features = branches[name];

            parts.Add(features.Read(index));
        }

        return torch.cat(parts, dim: 1).MoveToOuterDisposeScope();
    }

    private torch.Tensor Amplify(torch.Tensor features)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor doubledFeatures = Shuffle(upconv1, features);
        using torch.Tensor quadrupledFeatures = Shuffle(upconv2, doubledFeatures);
        using torch.Tensor highResolutionFeatures = functional.leaky_relu(
            convHr.call(quadrupledFeatures), NegativeSlope, inplace: true);

        torch.Tensor result = convLast.call(highResolutionFeatures);

        return result.MoveToOuterDisposeScope();
    }

    private static torch.Tensor Shuffle(Conv2d convolution, torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor result = functional.leaky_relu(
            functional.pixel_shuffle(convolution.call(input), 2), NegativeSlope, inplace: true);

        return result.MoveToOuterDisposeScope();
    }
}
