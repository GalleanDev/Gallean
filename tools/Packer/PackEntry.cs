namespace Packer;

internal abstract partial record PackEntry(string Id)
{
    public abstract void CopyTo(AssetPack pack, AssetPack.Writer writer);

    public static IEnumerable<PackEntry> EnumerateAll(AssetPack pack)
    {
        foreach (PackImageEntry entry in PackImageEntry.Enumerate(pack))
        {
            yield return entry;
        }

        foreach (AssetManifestSound sound in pack.Manifest.Sounds)
        {
            yield return new SoundEntry(sound);
        }
    }
}
