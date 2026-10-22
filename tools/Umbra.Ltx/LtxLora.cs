using TorchSharp;

namespace Umbra.Ltx;

internal sealed class LtxLora(Dictionary<string, LtxLoraEntry> entries, double strength) : IDisposable
{
    public void FuseInto(string prefix, IReadOnlyDictionary<string, torch.Tensor> weights)
    {
        using (torch.no_grad())
        {
            foreach ((string name, torch.Tensor weight) in weights)
            {
                if (!entries.TryGetValue(prefix + name, out LtxLoraEntry? entry))
                {
                    continue;
                }

                using torch.Tensor delta = Delta(weight, entry.Down, entry.Up, strength);

                weight.add_(delta);
            }
        }
    }

    public static torch.Tensor Delta(torch.Tensor weight, torch.Tensor down, torch.Tensor up, double strength)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor castDown = down.to(weight.dtype, weight.device);
        using torch.Tensor castUp = up.to(weight.dtype, weight.device);
        using torch.Tensor scaledUp = castUp.mul(strength);

        return torch.matmul(scaledUp, castDown).MoveToOuterDisposeScope();
    }

    public static torch.Tensor Fuse(torch.Tensor weight, torch.Tensor down, torch.Tensor up, double strength)
    {
        using IDisposable scope = torch.NewDisposeScope();
        using torch.Tensor delta = Delta(weight, down, up, strength);

        return delta.add(weight).MoveToOuterDisposeScope();
    }

    public void Dispose()
    {
        foreach (LtxLoraEntry entry in entries.Values)
        {
            entry.Down.Dispose();
            entry.Up.Dispose();
        }

        entries.Clear();
    }
}
