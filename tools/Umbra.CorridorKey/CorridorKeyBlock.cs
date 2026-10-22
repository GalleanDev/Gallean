using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.CorridorKey;

internal sealed class CorridorKeyBlock : Module<torch.Tensor, torch.Tensor>
{
    [ComponentName(Name = "norm1")]
    private readonly LayerNorm attentionNorm;

    [ComponentName(Name = "proj")]
    private readonly Linear? expansion;

    [ComponentName(Name = "attn")]
    private readonly CorridorKeyAttention attention;

    [ComponentName(Name = "norm2")]
    private readonly LayerNorm mlpNorm;

    [ComponentName(Name = "mlp")]
    private readonly CorridorKeyMlp mlp;

    public CorridorKeyBlock(
        int dimension,
        int outputDimension,
        int heads,
        double mlpRatio,
        int queryStride,
        int windowSize,
        bool maskUnitAttention)
        : base(nameof(CorridorKeyBlock))
    {
        attentionNorm = LayerNorm([dimension], eps: CorridorKeyOptions.NormEpsilon);

        if (dimension != outputDimension)
        {
            expansion = Linear(dimension, outputDimension, hasBias: true);
        }

        attention = new CorridorKeyAttention(dimension, outputDimension, heads, queryStride, windowSize, maskUnitAttention);
        mlpNorm = LayerNorm([outputDimension], eps: CorridorKeyOptions.NormEpsilon);
        mlp = new CorridorKeyMlp(outputDimension, (int)(outputDimension * mlpRatio));

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor tokens)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor normalizedTokens = attentionNorm.call(tokens);
        using torch.Tensor residual = Expand(tokens, normalizedTokens);
        using torch.Tensor attentionOutput = attention.call(normalizedTokens);
        using torch.Tensor hidden = residual.add(attentionOutput);
        using torch.Tensor mlpInput = mlpNorm.call(hidden);
        using torch.Tensor mlpOutput = mlp.call(mlpInput);

        return hidden.add(mlpOutput).MoveToOuterDisposeScope();
    }

    private torch.Tensor Expand(torch.Tensor tokens, torch.Tensor normalizedTokens)
    {
        using IDisposable scope = torch.NewDisposeScope();

        if (expansion is not null)
        {
            using torch.Tensor expansionOutput = expansion.call(normalizedTokens);

            return expansionOutput
                .reshape(expansionOutput.shape[0], attention.QueryStride, -1, expansionOutput.shape[2])
                .amax([1], keepdim: false)
                .MoveToOuterDisposeScope();
        }
        else
        {
            return tokens.alias().MoveToOuterDisposeScope();
        }
    }
}
