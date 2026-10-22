namespace Packer;

internal abstract partial record PackEntry
{
    private sealed record SoundEntry(AssetManifestSound Manifest) : PackEntry(Manifest.Id)
    {
        public override void CopyTo(AssetPack pack, AssetPack.Writer writer)
        {
            writer.AddSound(Id, new AssetSound(Manifest.Metadata, pack.OpenSound(Manifest)));
        }
    }
}
