using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;

namespace Umbra.Ltx;

internal sealed class LtxRmsNorm : Module<torch.Tensor, torch.Tensor>, IParameterModule
{
    public const double Epsilon = 1e-6;

    private const long ChunkTokens = 8192;

    private Parameter weight;

    public LtxRmsNorm(int dimension)
        : base(nameof(LtxRmsNorm))
    {
        weight = new Parameter(torch.ones(dimension), requires_grad: false);

        RegisterComponents();
    }

    public static torch.Tensor Normalize(torch.Tensor input, double epsilon)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long tokenDimension = input.shape.Length - 2;
        long tokens = input.shape[tokenDimension];

        torch.Tensor output = torch.empty_like(input);

        for (long start = 0; start < tokens; start += ChunkTokens)
        {
            long count = Math.Min(ChunkTokens, tokens - start);

            using IDisposable chunkScope = torch.NewDisposeScope();

            using torch.Tensor chunk = input.narrow(tokenDimension, start, count);
            using torch.Tensor floatChunk = chunk.to(torch.ScalarType.Float32);
            using torch.Tensor squares = floatChunk.pow(2.0);
            using torch.Tensor mean = squares.mean([-1], keepdim: true);
            using torch.Tensor scale = mean.add(epsilon).rsqrt();
            using torch.Tensor normalizedFloatChunk = floatChunk.mul(scale);
            using torch.Tensor normalizedChunk = normalizedFloatChunk.to(input.dtype);
            using torch.Tensor target = output.narrow(tokenDimension, start, count);

            target.copy_(normalizedChunk);
        }

        return output.MoveToOuterDisposeScope();
    }

    public void SetParameter(string name, Parameter value)
    {
        switch (name)
        {
            case "weight":
                weight = this.ReplaceParameter(weight, name, value);
                break;

            default:
                throw new NotSupportedException($"The parameter '{name}' of the '{GetName()}' module is not supported.");
        }
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor normalizedInput = Normalize(input, Epsilon);

        return normalizedInput.mul(weight).MoveToOuterDisposeScope();
    }
}
