using TorchSharp;

namespace Umbra;

internal sealed class TeaCache(TeaCacheOptions options) : IDisposable
{
    private readonly TeaCacheState even = new();
    private readonly TeaCacheState odd = new();
    private int forward;

    public torch.Tensor Apply(
        torch.Tensor working,
        torch.Tensor modulation,
        Func<torch.Tensor, torch.Tensor> transform)
    {
        if (options.Threshold <= 0)
        {
            return transform(working);
        }

        TeaCacheState state = GetCurrent();

        bool shouldCompute = state.ShouldCompute(modulation, options, forward);

        forward++;

        if (shouldCompute)
        {
            using torch.Tensor input = working;

            torch.Tensor result = transform(working.alias());
            state.SetResidual(result.sub(input));

            return result;
        }
        else
        {
            torch.Tensor result = working.add(state.GetResidual());

            working.Dispose();

            return result;
        }
    }

    public void Reset()
    {
        even.Reset();
        odd.Reset();
        forward = 0;
    }

    public void Dispose()
    {
        even.Dispose();
        odd.Dispose();
    }

    private TeaCacheState GetCurrent()
    {
        if (forward % 2 == 0)
        {
            return even;
        }
        else
        {
            return odd;
        }
    }
}
