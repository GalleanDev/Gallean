using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.Ltx;

internal sealed class LtxAttention : Module
{
    private readonly int heads;
    private readonly long chunkQueries;

    [ComponentName(Name = "q_norm")]
    private readonly LtxRmsNorm queryNorm;

    [ComponentName(Name = "k_norm")]
    private readonly LtxRmsNorm keyNorm;

    [ComponentName(Name = "to_q")]
    private readonly Linear toQuery;

    [ComponentName(Name = "to_k")]
    private readonly Linear toKey;

    [ComponentName(Name = "to_v")]
    private readonly Linear toValue;

    [ComponentName(Name = "to_gate_logits")]
    private readonly Linear toGateLogits;

    [ComponentName(Name = "to_out")]
    private readonly Sequential toOutput;

    public LtxAttention(int queryChannels, int contextChannels, int heads, int headDimension, long chunkQueries)
        : base(nameof(LtxAttention))
    {
        this.heads = heads;
        this.chunkQueries = Math.Max(1, chunkQueries);

        int innerChannels = heads * headDimension;

        queryNorm = new LtxRmsNorm(innerChannels);
        keyNorm = new LtxRmsNorm(innerChannels);
        toQuery = Linear(queryChannels, innerChannels, hasBias: true);
        toKey = Linear(contextChannels, innerChannels, hasBias: true);
        toValue = Linear(contextChannels, innerChannels, hasBias: true);
        toGateLogits = Linear(queryChannels, heads, hasBias: true);
        toOutput = Sequential(Linear(innerChannels, queryChannels, hasBias: true));

        RegisterComponents();
    }

    public torch.Tensor Forward(torch.Tensor input, torch.Tensor context, torch.Tensor? ropeCos, torch.Tensor? ropeSin)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long batch = input.shape[0];
        long tokens = input.shape[1];
        long headDimension = context.shape[2] / heads;

        using torch.Tensor key = ProjectKey(context, ropeCos, ropeSin);
        using torch.Tensor value = toValue.forward(context);
        using torch.Tensor headedKey = key.view(batch, -1, heads, headDimension).transpose(1, 2);
        using torch.Tensor headedValue = value.view(batch, -1, heads, headDimension).transpose(1, 2);

        torch.Tensor output = torch.empty([batch, tokens, heads * headDimension], input.dtype, input.device);

        for (long start = 0; start < tokens; start += chunkQueries)
        {
            long count = Math.Min(chunkQueries, tokens - start);

            using IDisposable chunkScope = torch.NewDisposeScope();

            using torch.Tensor chunk = input.narrow(1, start, count);
            using torch.Tensor query = ProjectQuery(chunk, ropeCos, ropeSin, start, count);
            using torch.Tensor headedQuery = query.view(batch, count, heads, headDimension).transpose(1, 2);
            using torch.Tensor attention = functional.scaled_dot_product_attention(headedQuery, headedKey, headedValue);
            using torch.Tensor gatedAttention = Gate(chunk, attention);
            using torch.Tensor projection = toOutput.forward(gatedAttention);
            using torch.Tensor target = output.narrow(1, start, count);

            target.copy_(projection);
        }

        return output.MoveToOuterDisposeScope();
    }

    private torch.Tensor ProjectKey(torch.Tensor context, torch.Tensor? ropeCos, torch.Tensor? ropeSin)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor key;

        using (torch.Tensor projection = toKey.forward(context))
        {
            key = keyNorm.forward(projection);
        }

        if (ropeCos is not null && ropeSin is not null)
        {
            long tokens = key.shape[1];

            for (long start = 0; start < tokens; start += chunkQueries)
            {
                long count = Math.Min(chunkQueries, tokens - start);

                using IDisposable chunkScope = torch.NewDisposeScope();

                using torch.Tensor chunk = key.narrow(1, start, count);
                using torch.Tensor cos = ropeCos.narrow(2, start, count);
                using torch.Tensor sin = ropeSin.narrow(2, start, count);
                using torch.Tensor rotated = LtxRope.Apply(chunk, cos, sin, heads);

                chunk.copy_(rotated);
            }
        }

        return key.MoveToOuterDisposeScope();
    }

    private torch.Tensor ProjectQuery(torch.Tensor chunk, torch.Tensor? ropeCos, torch.Tensor? ropeSin, long start, long count)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor query = toQuery.forward(chunk);
        using torch.Tensor normalizedQuery = queryNorm.forward(query);

        if (ropeCos is not null && ropeSin is not null)
        {
            using torch.Tensor cos = ropeCos.narrow(2, start, count);
            using torch.Tensor sin = ropeSin.narrow(2, start, count);

            return LtxRope.Apply(normalizedQuery, cos, sin, heads).MoveToOuterDisposeScope();
        }
        else
        {
            return normalizedQuery.alias().MoveToOuterDisposeScope();
        }
    }

    private torch.Tensor Gate(torch.Tensor input, torch.Tensor attention)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long batch = input.shape[0];
        long tokens = input.shape[1];
        long headDimension = attention.shape[3];

        using torch.Tensor logits = toGateLogits.forward(input);
        using torch.Tensor gates = torch.sigmoid(logits).mul(2.0);
        using torch.Tensor gatedHeads = attention.transpose(1, 2).mul(gates.unsqueeze(-1));

        return gatedHeads.reshape(batch, tokens, heads * headDimension).MoveToOuterDisposeScope();
    }
}
