namespace Gallean.Client;

internal sealed partial record WorldRenderState
{
    public sealed record AmbientSound(
        AssetSoundKey AssetKey,
        WorldFootprint Footprint);
}
