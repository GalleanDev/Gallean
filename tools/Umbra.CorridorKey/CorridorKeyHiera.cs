using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.CorridorKey;

internal sealed class CorridorKeyHiera : Module, IParameterModule
{
    [ComponentName(Name = "patch_embed")]
    private readonly CorridorKeyPatchEmbed patchEmbed;

    [ComponentName(Name = "pos_embed")]
    private Parameter positionEmbedding;

    [ComponentName(Name = "blocks")]
    private readonly ModuleList<CorridorKeyBlock> blocks;

    private readonly CorridorKeyOptions options;
    private readonly IReadOnlyList<int> stageEnds;

    public CorridorKeyHiera(CorridorKeyOptions options)
        : base(nameof(CorridorKeyHiera))
    {
        this.options = options;

        stageEnds = options.StageEnds;

        long grid = options.TokenGrid;

        patchEmbed = new CorridorKeyPatchEmbed(
            options.InputChannels,
            options.EmbedDimension,
            options.PatchKernel,
            options.PatchStride,
            options.PatchPadding);
        positionEmbedding = new Parameter(
            torch.zeros(1, grid * grid, options.EmbedDimension), requires_grad: false);
        blocks = new ModuleList<CorridorKeyBlock>();

        int dimension = options.EmbedDimension;
        int heads = options.Heads;
        int maskUnitSize = options.FlatMaskUnitSize;
        int stage = 0;

        for (int index = 0; index < options.Depth; index++)
        {
            bool maskUnitAttention = options.MaskUnitAttention[stage];
            bool pooled = IsPoolBlock(index);
            int outputDimension = dimension;
            int queryStride = 1;

            if (index > 0 && stageEnds.Contains(index - 1))
            {
                outputDimension = dimension * CorridorKeyOptions.DimensionMultiplier;
                heads *= CorridorKeyOptions.HeadMultiplier;
                stage++;

                if (pooled)
                {
                    maskUnitSize /= options.FlatQueryStride;
                }
            }

            if (pooled)
            {
                queryStride = options.FlatQueryStride;
            }

            blocks.Add(new CorridorKeyBlock(
                dimension,
                outputDimension,
                heads,
                options.MlpRatio,
                queryStride,
                maskUnitSize,
                maskUnitAttention));

            dimension = outputDimension;
        }

        RegisterComponents();
    }

    public void SetParameter(string name, Parameter value)
    {
        switch (name)
        {
            case "pos_embed":
                positionEmbedding = this.ReplaceParameter(positionEmbedding, name, value);
                break;

            default:
                throw new NotSupportedException($"The parameter '{name}' of the '{GetName()}' module is not supported.");
        }
    }

    public IReadOnlyList<torch.Tensor> Forward(torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        List<torch.Tensor> features = [];

        using torch.Tensor patches = patchEmbed.call(input);
        using torch.Tensor positionedPatches = patches.add(positionEmbedding);

        torch.Tensor tokens = CorridorKeyUnroll.Forward(
            positionedPatches, options.TokenGrid, CorridorKeyOptions.QueryStride, stageEnds.Count - 1);

        for (int index = 0; index < blocks.Count; index++)
        {
            torch.Tensor next = blocks[index].call(tokens);

            tokens.Dispose();
            tokens = next;

            int stage = FindStage(index);

            if (stage >= 0)
            {
                features.Add(RerollStage(tokens, stage));
            }
        }

        tokens.Dispose();

        List<torch.Tensor> result = [];

        foreach (torch.Tensor feature in features)
        {
            result.Add(feature.MoveToOuterDisposeScope());
        }

        return result;
    }

    private int FindStage(int index)
    {
        for (int stage = 0; stage < stageEnds.Count; stage++)
        {
            if (stageEnds[stage] == index)
            {
                return stage;
            }
        }

        return -1;
    }

    private bool IsPoolBlock(int index)
    {
        for (int stage = 0; stage < options.PoolStages; stage++)
        {
            if (stageEnds[stage] + 1 == index)
            {
                return true;
            }
        }

        return false;
    }

    private torch.Tensor RerollStage(torch.Tensor tokens, int stage)
    {
        using IDisposable scope = torch.NewDisposeScope();

        int consumed = Math.Min(stage, options.PoolStages);
        int steps = stageEnds.Count - 1 - consumed;
        long grid = options.TokenGrid;

        for (int step = 0; step < consumed; step++)
        {
            grid /= CorridorKeyOptions.QueryStride;
        }

        using torch.Tensor intermediate = CorridorKeyReroll.Forward(tokens, grid, CorridorKeyOptions.QueryStride, steps);

        return intermediate.permute(0, 3, 1, 2).contiguous().MoveToOuterDisposeScope();
    }
}
