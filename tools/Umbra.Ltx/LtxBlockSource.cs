using TorchSharp;
using static TorchSharp.torch.nn;

namespace Umbra.Ltx;

internal sealed class LtxBlockSource : IDisposable
{
    private readonly IReadOnlyList<Dictionary<string, torch.Tensor>> blocks;

    private LtxBlockSource(IReadOnlyList<Dictionary<string, torch.Tensor>> blocks)
    {
        this.blocks = blocks;

        Layout = LtxBlockLayout.Derive(blocks[0]);

        foreach (Dictionary<string, torch.Tensor> block in blocks)
        {
            LtxBlockLayout layout = LtxBlockLayout.Derive(block);

            if (layout.Elements > Layout.Elements)
            {
                Layout = layout;
            }
        }
    }

    public int Count => blocks.Count;

    public LtxBlockLayout Layout { get; }

    public static LtxBlockSource Capture(IEnumerable<Module> blocks)
    {
        List<Dictionary<string, torch.Tensor>> states = [];

        foreach (Module block in blocks)
        {
            Dictionary<string, torch.Tensor> captured = [];

            foreach ((string name, torch.Tensor weight) in block.state_dict())
            {
                torch.Tensor alias = weight.alias();

                alias.DetachFromDisposeScope();

                captured[name] = alias;
            }

            states.Add(captured);
        }

        return new LtxBlockSource(states);
    }

    public IReadOnlyDictionary<string, torch.Tensor> GetBlock(int index)
    {
        return blocks[index];
    }

    public void Dispose()
    {
        foreach (Dictionary<string, torch.Tensor> block in blocks)
        {
            foreach (torch.Tensor tensor in block.Values)
            {
                tensor.Dispose();
            }

            block.Clear();
        }
    }
}
