using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;
using GELU = TorchSharp.Modules.GELU;

namespace Umbra;

internal sealed class WanDitBlock :
    Module<torch.Tensor, torch.Tensor, torch.Tensor, torch.Tensor, torch.Tensor>,
    IParameterModule
{
    private const long ChunkLimit = 4 * 1024;

    [ComponentName(Name = "self_attn")]
    private readonly WanSelfAttention selfAttention;

    [ComponentName(Name = "cross_attn")]
    private readonly WanCrossAttention crossAttention;

    private readonly LayerNorm norm1;
    private readonly LayerNorm norm2;
    private readonly LayerNorm norm3;
    private readonly ModuleList<Module<torch.Tensor, torch.Tensor>> ffn;
    private Parameter modulation;

    public WanDitBlock(int dim, int heads, int feedForwardDim, double epsilon = 1e-6)
        : base(nameof(WanDitBlock))
    {
        selfAttention = new WanSelfAttention(dim, heads, epsilon);
        crossAttention = new WanCrossAttention(dim, heads, epsilon);
        norm1 = LayerNorm([dim], eps: epsilon, elementwise_affine: false);
        norm2 = LayerNorm([dim], eps: epsilon, elementwise_affine: false);
        norm3 = LayerNorm([dim], eps: epsilon);
        ffn = new ModuleList<Module<torch.Tensor, torch.Tensor>>(
            Linear(dim, feedForwardDim),
            GELU(approximate: GELU.Approximate.tanh),
            Linear(feedForwardDim, dim));
        modulation = new Parameter(torch.zeros(1, 6, dim), requires_grad: false);

        RegisterComponents();
    }

    public void SetParameter(string name, Parameter value)
    {
        switch (name)
        {
            case "modulation":
                modulation = this.ReplaceParameter(modulation, name, value);
                break;

            default:
                throw new NotSupportedException($"The parameter '{name}' of the '{GetName()}' module is not supported.");
        }
    }

    public override torch.Tensor forward(
        torch.Tensor input,
        torch.Tensor context,
        torch.Tensor timeModulation,
        torch.Tensor frequencies)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor blockModulation = modulation.to(timeModulation.dtype, timeModulation.device).add(timeModulation);

        torch.Tensor[] parts = blockModulation.chunk(6, dim: 1);

        using torch.Tensor shiftAttention = parts[0];
        using torch.Tensor scaleAttention = parts[1];
        using torch.Tensor gateAttention = parts[2];
        using torch.Tensor shiftFeedForward = parts[3];
        using torch.Tensor scaleFeedForward = parts[4];
        using torch.Tensor gateFeedForward = parts[5];

        using torch.Tensor afterAttention = AddAttention(
            input,
            shiftAttention,
            scaleAttention,
            gateAttention,
            frequencies);

        using torch.Tensor afterCross = AddCrossAttention(afterAttention, context);

        torch.Tensor result = AddFeedForward(
            afterCross,
            shiftFeedForward,
            scaleFeedForward,
            gateFeedForward);

        return result.MoveToOuterDisposeScope();
    }

    private torch.Tensor AddAttention(
        torch.Tensor input,
        torch.Tensor shift,
        torch.Tensor scale,
        torch.Tensor gate,
        torch.Tensor frequencies)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor normalizedInput = norm1.call(input);
        using torch.Tensor attentionInput = Modulate(normalizedInput, shift, scale);
        using torch.Tensor attention = selfAttention.call(attentionInput, frequencies);
        using torch.Tensor gatedAttention = attention.mul(gate);

        return input.add(gatedAttention).MoveToOuterDisposeScope();
    }

    private torch.Tensor AddCrossAttention(torch.Tensor input, torch.Tensor context)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor normalizedInput = norm3.call(input);
        using torch.Tensor attention = crossAttention.call(normalizedInput, context);

        return input.add(attention).MoveToOuterDisposeScope();
    }

    private torch.Tensor AddFeedForward(
        torch.Tensor input,
        torch.Tensor shift,
        torch.Tensor scale,
        torch.Tensor gate)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor normalizedInput = norm2.call(input);
        using torch.Tensor feedForwardInput = Modulate(normalizedInput, shift, scale);
        using torch.Tensor feedForward = TensorOperations.RunChunked(feedForwardInput, ChunkLimit, 1, ApplyChunk);
        using torch.Tensor gatedFeedForward = feedForward.mul(gate);

        return input.add(gatedFeedForward).MoveToOuterDisposeScope();
    }

    private torch.Tensor ApplyChunk(torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor result = input.alias();

        foreach (Module<torch.Tensor, torch.Tensor> layer in ffn)
        {
            torch.Tensor next = layer.call(result);

            result.Dispose();
            result = next;
        }

        return result.MoveToOuterDisposeScope();
    }

    private static torch.Tensor Modulate(torch.Tensor input, torch.Tensor shift, torch.Tensor scale)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor factor = scale.add(1.0);
        using torch.Tensor scaledInput = input.mul(factor);

        torch.Tensor result = scaledInput.add(shift);

        return result.MoveToOuterDisposeScope();
    }
}
