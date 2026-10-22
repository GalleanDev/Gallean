using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.Ltx;

internal sealed class LtxTransformerBlock : Module, IParameterModule
{
    private const int AttentionModulation = 0;
    private const int MlpModulation = 3;
    private const int CrossModulation = 6;

    private readonly int hiddenSize;
    private readonly long mlpChunkTokens;

    [ComponentName(Name = "scale_shift_table")]
    private Parameter scaleShiftTable;

    [ComponentName(Name = "prompt_scale_shift_table")]
    private Parameter promptScaleShiftTable;

    [ComponentName(Name = "attn1")]
    private readonly LtxAttention selfAttention;

    [ComponentName(Name = "attn2")]
    private readonly LtxAttention crossAttention;

    [ComponentName(Name = "ff")]
    private readonly LtxFeedForward feedForward;

    public LtxTransformerBlock(
        int hiddenSize,
        int heads,
        int headDimension,
        int contextChannels,
        long mlpChunkTokens,
        long attentionChunkQueries)
        : base(nameof(LtxTransformerBlock))
    {
        this.hiddenSize = hiddenSize;
        this.mlpChunkTokens = Math.Max(1, mlpChunkTokens);

        selfAttention = new LtxAttention(hiddenSize, hiddenSize, heads, headDimension, attentionChunkQueries);
        crossAttention = new LtxAttention(hiddenSize, contextChannels, heads, headDimension, attentionChunkQueries);
        feedForward = new LtxFeedForward(hiddenSize, 4, hasBias: false);
        scaleShiftTable = new Parameter(torch.zeros(LtxDitOptions.ModulationCount, hiddenSize), requires_grad: false);
        promptScaleShiftTable = new Parameter(torch.zeros(LtxDitOptions.PromptModulationCount, hiddenSize), requires_grad: false);

        RegisterComponents();
    }

    public void SetParameter(string name, Parameter value)
    {
        switch (name)
        {
            case "scale_shift_table":
                scaleShiftTable = this.ReplaceParameter(scaleShiftTable, name, value);
                break;

            case "prompt_scale_shift_table":
                promptScaleShiftTable = this.ReplaceParameter(promptScaleShiftTable, name, value);
                break;

            default:
                throw new NotSupportedException($"The parameter '{name}' of the '{GetName()}' module is not supported.");
        }
    }

    public torch.Tensor Forward(
        torch.Tensor input,
        torch.Tensor context,
        torch.Tensor modulation,
        LtxTimestepSelection selection,
        torch.Tensor promptModulation,
        torch.Tensor ropeCos,
        torch.Tensor ropeSin)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor rows = modulation.view(modulation.shape[0], LtxDitOptions.ModulationCount, hiddenSize);
        using torch.Tensor table = scaleShiftTable.to(input.dtype);

        torch.Tensor current;

        using (torch.Tensor normalizedInput = Modulate(input, rows, table, selection, AttentionModulation))
        using (torch.Tensor attention = selfAttention.Forward(normalizedInput, normalizedInput, ropeCos, ropeSin))
        {
            Gate(attention, rows, table, selection, AttentionModulation);

            current = input.add(attention);
        }

        using (torch.Tensor normalized = Modulate(current, rows, table, selection, CrossModulation))
        {
            using torch.Tensor promptTable = promptScaleShiftTable.view(1, 1, LtxDitOptions.PromptModulationCount, hiddenSize).to(input.dtype);
            using torch.Tensor promptValues = promptTable.add(promptModulation);
            using torch.Tensor contextShift = promptValues.select(2, 0);
            using torch.Tensor contextScale = promptValues.select(2, 1);
            using torch.Tensor modulatedContext = context.mul(contextScale.add(1.0)).add(contextShift);
            using torch.Tensor attention = crossAttention.Forward(normalized, modulatedContext, null, null);

            Gate(attention, rows, table, selection, CrossModulation);
            current.add_(attention);
        }

        using torch.Tensor tokens = current.view(-1, hiddenSize);

        foreach (LtxTimestepRun run in selection.Runs)
        {
            using torch.Tensor shift = Row(rows, table, run.Value, MlpModulation, 0);
            using torch.Tensor scale = Row(rows, table, run.Value, MlpModulation, 1);
            using torch.Tensor gate = Row(rows, table, run.Value, MlpModulation, 2);
            using torch.Tensor factor = scale.add(1.0);

            for (long start = run.Start; start < run.Start + run.Count; start += mlpChunkTokens)
            {
                long count = Math.Min(mlpChunkTokens, run.Start + run.Count - start);

                using IDisposable chunkScope = torch.NewDisposeScope();

                using torch.Tensor chunk = tokens.narrow(0, start, count);
                using torch.Tensor normalizedChunk = LtxRmsNorm.Normalize(chunk, LtxRmsNorm.Epsilon);

                normalizedChunk.mul_(factor).add_(shift);

                using torch.Tensor feedForwardOutput = feedForward.forward(normalizedChunk);

                feedForwardOutput.mul_(gate);
                chunk.add_(feedForwardOutput);
            }
        }

        return current.MoveToOuterDisposeScope();
    }

    private static torch.Tensor Row(torch.Tensor rows, torch.Tensor table, long value, int group, int offset)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor modulationRow = rows.select(0, value).select(0, group + offset);
        using torch.Tensor fixedRow = table.select(0, group + offset);

        return modulationRow.add(fixedRow).MoveToOuterDisposeScope();
    }

    private torch.Tensor Modulate(
        torch.Tensor input,
        torch.Tensor rows,
        torch.Tensor table,
        LtxTimestepSelection selection,
        int group)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor result = LtxRmsNorm.Normalize(input, LtxRmsNorm.Epsilon);

        using torch.Tensor tokens = result.view(-1, hiddenSize);

        foreach (LtxTimestepRun run in selection.Runs)
        {
            using torch.Tensor shift = Row(rows, table, run.Value, group, 0);
            using torch.Tensor scale = Row(rows, table, run.Value, group, 1);
            using torch.Tensor factor = scale.add(1.0);
            using torch.Tensor segment = tokens.narrow(0, run.Start, run.Count);

            segment.mul_(factor).add_(shift);
        }

        return result.MoveToOuterDisposeScope();
    }

    private void Gate(torch.Tensor attention, torch.Tensor rows, torch.Tensor table, LtxTimestepSelection selection, int group)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor tokens = attention.view(-1, hiddenSize);

        foreach (LtxTimestepRun run in selection.Runs)
        {
            using torch.Tensor gate = Row(rows, table, run.Value, group, 2);
            using torch.Tensor segment = tokens.narrow(0, run.Start, run.Count);

            segment.mul_(gate);
        }
    }
}
