namespace Packer;

internal sealed class PackFilterSource(AssetPack pack) : ISpriteFilterSource
{
    private readonly IReadOnlyDictionary<string, PackImageEntry> entries = GetEntries(pack);

    public IReadOnlyList<AssetSprite>? ReadFrames(AssetKey key)
    {
        if (entries.TryGetValue(key.CanonicalId, out PackImageEntry? entry))
        {
            return entry.ReadFrames(pack);
        }
        else
        {
            return null;
        }
    }

    private static IReadOnlyDictionary<string, PackImageEntry> GetEntries(AssetPack pack)
    {
        Dictionary<string, PackImageEntry> entries = [];

        foreach (PackImageEntry entry in PackImageEntry.Enumerate(pack))
        {
            entries.Add(entry.Id, entry);
        }

        return entries;
    }
}
