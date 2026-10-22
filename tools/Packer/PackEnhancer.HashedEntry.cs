namespace Packer;

internal sealed partial class PackEnhancer
{
    private sealed record HashedEntry(
        PackImageEntry Entry,
        IReadOnlyList<UmbraSpriteHash> FrameHashes,
        IReadOnlyList<EnhancementType> Enhancements,
        UpscalerType? Upscaler,
        UmbraPromptHash PromptHash);
}
