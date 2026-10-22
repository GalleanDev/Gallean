using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.CorridorKey;

internal sealed class CorridorKeyAttention : Module<torch.Tensor, torch.Tensor>
{
    [ComponentName(Name = "qkv")]
    private readonly Linear queryKeyValue;

    [ComponentName(Name = "proj")]
    private readonly Linear projection;

    private readonly int outputDimension;
    private readonly int heads;
    private readonly int headDimension;
    private readonly int windowSize;
    private readonly bool maskUnitAttention;

    public CorridorKeyAttention(
        int dimension,
        int outputDimension,
        int heads,
        int queryStride,
        int windowSize,
        bool maskUnitAttention)
        : base(nameof(CorridorKeyAttention))
    {
        this.outputDimension = outputDimension;
        this.heads = heads;
        this.windowSize = windowSize;
        this.maskUnitAttention = maskUnitAttention;

        QueryStride = queryStride;
        headDimension = outputDimension / heads;

        queryKeyValue = Linear(dimension, 3 * outputDimension, hasBias: true);
        projection = Linear(outputDimension, outputDimension, hasBias: true);

        RegisterComponents();
    }

    public int QueryStride { get; }

    public override torch.Tensor forward(torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor queryKeyValueTokens = queryKeyValue.call(input);

        if (maskUnitAttention)
        {
            return AttendWindows(queryKeyValueTokens, input.shape[0], input.shape[1]).MoveToOuterDisposeScope();
        }
        else
        {
            return AttendGlobally(queryKeyValueTokens, input.shape[0], input.shape[1]).MoveToOuterDisposeScope();
        }
    }

    private torch.Tensor AttendWindows(torch.Tensor queryKeyValueTokens, long batch, long tokens)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long windows = tokens / (QueryStride * windowSize);

        using torch.Tensor queryKeyValueHeads = queryKeyValueTokens
            .reshape(batch, -1, windows, 3, heads, headDimension)
            .permute(3, 0, 4, 2, 1, 5);
        using torch.Tensor fullQuery = queryKeyValueHeads.select(0, 0);
        using torch.Tensor query = PoolQuery(fullQuery, 3);
        using torch.Tensor key = queryKeyValueHeads.select(0, 1).contiguous();
        using torch.Tensor value = queryKeyValueHeads.select(0, 2).contiguous();
        using torch.Tensor context = functional.scaled_dot_product_attention(query, key, value);
        using torch.Tensor contextTokens = context.transpose(1, 3).reshape(batch, -1, outputDimension);

        return projection.call(contextTokens).MoveToOuterDisposeScope();
    }

    private torch.Tensor AttendGlobally(torch.Tensor queryKeyValueTokens, long batch, long tokens)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor queryKeyValueHeads = queryKeyValueTokens
            .reshape(batch, tokens, 3, heads, headDimension)
            .permute(2, 0, 3, 1, 4);
        using torch.Tensor fullQuery = queryKeyValueHeads.select(0, 0);
        using torch.Tensor query = PoolQuery(fullQuery, 2);
        using torch.Tensor key = queryKeyValueHeads.select(0, 1).contiguous();
        using torch.Tensor value = queryKeyValueHeads.select(0, 2).contiguous();
        using torch.Tensor context = functional.scaled_dot_product_attention(query, key, value);
        using torch.Tensor contextTokens = context.transpose(1, 2).reshape(batch, -1, outputDimension);

        return projection.call(contextTokens).MoveToOuterDisposeScope();
    }

    private torch.Tensor PoolQuery(torch.Tensor query, int poolDimension)
    {
        using IDisposable scope = torch.NewDisposeScope();

        if (QueryStride > 1)
        {
            long[] shape = [.. query.shape[..poolDimension], QueryStride, -1, headDimension];

            return query.reshape(shape).amax([poolDimension], keepdim: false).contiguous().MoveToOuterDisposeScope();
        }
        else
        {
            return query.contiguous().MoveToOuterDisposeScope();
        }
    }
}
