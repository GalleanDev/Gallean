using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed class WanSelfAttention : Module<torch.Tensor, torch.Tensor, torch.Tensor>
{
    private readonly int heads;

    private readonly Linear q;
    private readonly Linear k;
    private readonly Linear v;
    private readonly Linear o;

    [ComponentName(Name = "norm_q")]
    private readonly WanNorm normQ;

    [ComponentName(Name = "norm_k")]
    private readonly WanNorm normK;

    public WanSelfAttention(int dim, int heads, double epsilon = 1e-6)
        : base(nameof(WanSelfAttention))
    {
        this.heads = heads;

        q = Linear(dim, dim);
        k = Linear(dim, dim);
        v = Linear(dim, dim);
        o = Linear(dim, dim);
        normQ = new WanNorm(dim, epsilon);
        normK = new WanNorm(dim, epsilon);

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor input, torch.Tensor frequencies)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor query = normQ.call(q.call(input));
        using torch.Tensor key = normK.call(k.call(input));
        using torch.Tensor value = v.call(input);

        using torch.Tensor rotatedQuery = WanRotation.Apply(query, frequencies, heads);
        using torch.Tensor rotatedKey = WanRotation.Apply(key, frequencies, heads);
        using torch.Tensor attention = WanAttention.Apply(rotatedQuery, rotatedKey, value, heads);

        torch.Tensor result = o.call(attention);

        return result.MoveToOuterDisposeScope();
    }
}
