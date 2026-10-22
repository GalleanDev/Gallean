using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.Ltx;

internal sealed class LtxDit : Module, IParameterModule
{
    private const string BlocksPrefix = "transformer_blocks.";

    private readonly LtxDitOptions options;
    private LtxBlockWeights? blockWeights;

    [ComponentName(Name = "keyframes_abs_pos_embedding")]
    private Parameter keyframeEmbedding;

    [ComponentName(Name = "scale_shift_table")]
    private Parameter outputScaleShiftTable;

    [ComponentName(Name = "patchify_proj")]
    private readonly Linear patchProjection;

    [ComponentName(Name = "adaln_single")]
    private readonly LtxAdaLayerNorm timestepModulation;

    [ComponentName(Name = "prompt_adaln_single")]
    private readonly LtxAdaLayerNorm promptModulation;

    [ComponentName(Name = "transformer_blocks")]
    private readonly ModuleList<LtxTransformerBlock> blocks;

    [ComponentName(Name = "norm_out")]
    private readonly LayerNorm outputNorm;

    [ComponentName(Name = "proj_out")]
    private readonly Linear outputProjection;

    public LtxDit(LtxDitOptions options)
        : base(nameof(LtxDit))
    {
        this.options = options;

        patchProjection = Linear(options.InputChannels, options.HiddenSize, hasBias: true);
        keyframeEmbedding = new Parameter(torch.zeros(1, options.HiddenSize), requires_grad: false);
        timestepModulation = new LtxAdaLayerNorm(options.HiddenSize, LtxDitOptions.ModulationCount);
        promptModulation = new LtxAdaLayerNorm(options.HiddenSize, LtxDitOptions.PromptModulationCount);
        blocks = new ModuleList<LtxTransformerBlock>();

        for (int index = 0; index < options.Depth; index++)
        {
            blocks.Add(new LtxTransformerBlock(
                options.HiddenSize,
                options.Heads,
                options.HeadDimension,
                options.ContextChannels,
                options.MlpChunkTokens,
                options.AttentionChunkQueries));
        }

        outputScaleShiftTable = new Parameter(torch.zeros(LtxDitOptions.OutputModulationCount, options.HiddenSize), requires_grad: false);
        outputNorm = LayerNorm([options.HiddenSize], eps: 1e-6, elementwise_affine: false);
        outputProjection = Linear(options.HiddenSize, options.OutputChannels, hasBias: true);

        RegisterComponents();
    }

    public void SetParameter(string name, Parameter value)
    {
        switch (name)
        {
            case "keyframes_abs_pos_embedding":
                keyframeEmbedding = this.ReplaceParameter(keyframeEmbedding, name, value);
                break;

            case "scale_shift_table":
                outputScaleShiftTable = this.ReplaceParameter(outputScaleShiftTable, name, value);
                break;

            default:
                throw new NotSupportedException($"The parameter '{name}' of the '{GetName()}' module is not supported.");
        }
    }

    public void StreamBlocks(LtxLora? lora, int cachedBlocks, torch.ScalarType type, torch.Device device)
    {
        blockWeights = new LtxBlockWeights(LtxBlockSource.Capture(blocks), lora, BlocksPrefix, cachedBlocks, type, device);
    }

    public void MoveResidentTo(torch.ScalarType type, torch.Device device)
    {
        ModuleParameters.MoveTo(patchProjection, type, device);
        ModuleParameters.MoveTo(timestepModulation, type, device);
        ModuleParameters.MoveTo(promptModulation, type, device);
        ModuleParameters.MoveTo(outputNorm, type, device);
        ModuleParameters.MoveTo(outputProjection, type, device);

        keyframeEmbedding = MoveParameter(keyframeEmbedding, "keyframes_abs_pos_embedding", type, device);
        outputScaleShiftTable = MoveParameter(outputScaleShiftTable, "scale_shift_table", type, device);
    }

    public torch.Tensor Forward(
        torch.Tensor tokens,
        LtxTimestepSelection timesteps,
        torch.Tensor sigma,
        torch.Tensor positions,
        torch.Tensor context,
        torch.Tensor keyframesMask)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.ScalarType dtype = tokens.dtype;

        torch.Tensor embeddedTokens = Embed(tokens, keyframesMask, dtype);

        using torch.Tensor scaledTimesteps = timesteps.Values.mul(options.TimestepScale);

        (torch.Tensor modulation, torch.Tensor embedded) = timestepModulation.Forward(scaledTimesteps, dtype);

        using torch.Tensor ownedModulation = modulation;
        using torch.Tensor ownedEmbedded = embedded;
        using torch.Tensor scaledSigma = sigma.mul(options.TimestepScale);

        (torch.Tensor promptValues, torch.Tensor promptEmbedded) = promptModulation.Forward(scaledSigma, dtype);

        using torch.Tensor ownedPromptEmbedded = promptEmbedded;
        using torch.Tensor ownedPromptValues = promptValues;
        using torch.Tensor promptTimestep = ownedPromptValues.view(sigma.shape[0], 1, LtxDitOptions.PromptModulationCount, options.HiddenSize);

        (torch.Tensor ropeCos, torch.Tensor ropeSin) = LtxRope.GetFrequencies(
            positions, options.HiddenSize, options.Heads, options.RopeTheta, options.RopeMaxPositions, dtype);

        using torch.Tensor ownedCos = ropeCos;
        using torch.Tensor ownedSin = ropeSin;

        torch.Tensor current = embeddedTokens;

        for (int index = 0; index < blocks.Count; index++)
        {
            LtxTransformerBlock block = blocks[index];

            if (blockWeights is not null)
            {
                ModuleParameters.Attach(block, blockWeights.Get(index));
            }

            torch.Tensor next = block.Forward(current, context, ownedModulation, timesteps, promptTimestep, ownedCos, ownedSin);

            current.Dispose();
            current = next;
        }

        using torch.Tensor hidden = current;
        using torch.Tensor outputTable = outputScaleShiftTable.to(dtype);
        using torch.Tensor normalizedHidden = outputNorm.forward(hidden);
        using torch.Tensor normalizedRows = normalizedHidden.view(-1, options.HiddenSize);

        foreach (LtxTimestepRun run in timesteps.Runs)
        {
            using torch.Tensor embeddedRow = ownedEmbedded.select(0, run.Value);
            using torch.Tensor shift = embeddedRow.add(outputTable.select(0, 0));
            using torch.Tensor scale = embeddedRow.add(outputTable.select(0, 1));
            using torch.Tensor factor = scale.add(1.0);
            using torch.Tensor segment = normalizedRows.narrow(0, run.Start, run.Count);

            segment.mul_(factor).add_(shift);
        }

        return outputProjection.forward(normalizedHidden).MoveToOuterDisposeScope();
    }

    private torch.Tensor Embed(torch.Tensor tokens, torch.Tensor keyframesMask, torch.ScalarType dtype)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor keyframeFlags = keyframesMask.gt(0.0).to(dtype);
        using torch.Tensor marker = keyframeFlags.mul(keyframeEmbedding.to(dtype));

        torch.Tensor projection = patchProjection.forward(tokens);

        projection.add_(marker);

        return projection.MoveToOuterDisposeScope();
    }

    private Parameter MoveParameter(
        Parameter parameter,
        string name,
        torch.ScalarType type,
        torch.Device device)
    {
        if (parameter.dtype == type &&
            parameter.device_type == device.type &&
            parameter.device_index == device.index)
        {
            return parameter;
        }

        Parameter moved = new(parameter.to(type, device), requires_grad: false);

        moved.DetachFromDisposeScope();
        parameter.Dispose();
        ConditionallyRegisterParameter(name, moved);

        return moved;
    }
}
