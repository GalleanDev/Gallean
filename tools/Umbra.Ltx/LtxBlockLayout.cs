using TorchSharp;

namespace Umbra.Ltx;

internal sealed record LtxBlockLayout(IReadOnlyList<LtxBlockEntry> Entries, long Elements)
{
    public static LtxBlockLayout Derive(IReadOnlyDictionary<string, torch.Tensor> tensors)
    {
        List<LtxBlockEntry> entries = [];
        long offset = 0;

        foreach (string name in tensors.Keys.Order(StringComparer.Ordinal))
        {
            torch.Tensor tensor = tensors[name];
            long elements = tensor.NumberOfElements;

            entries.Add(new LtxBlockEntry(name, tensor.shape, offset, elements));
            offset += elements;
        }

        return new LtxBlockLayout(entries, offset);
    }

    public Dictionary<string, torch.Tensor> Carve(torch.Tensor buffer)
    {
        if (buffer.NumberOfElements < Elements)
        {
            throw new ArgumentException("Invalid number of elements in the buffer.");
        }

        Dictionary<string, torch.Tensor> views = [];

        foreach (LtxBlockEntry entry in Entries)
        {
            using torch.Tensor region = buffer.narrow(0, entry.Offset, entry.Elements);

            views[entry.Name] = region.view(entry.Shape);
        }

        return views;
    }
}
