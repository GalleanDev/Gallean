using TorchSharp;

namespace Umbra.Ltx;

internal sealed record LtxCachedBlock(torch.Tensor Buffer, Dictionary<string, torch.Tensor> Views)
{
    public void Dispose()
    {
        foreach (torch.Tensor view in Views.Values)
        {
            view.Dispose();
        }

        Views.Clear();
    }
}
