namespace Packer;

internal sealed partial class PackEnhancer
{
    private sealed record EnhancementOutput(
        IReadOnlyList<EnhancementType> Enhancements,
        UpscalerType? Upscaler,
        UmbraPromptHash? PromptHash)
    {
        public bool Matches(IReadOnlyList<EnhancementType> enhancements, UpscalerType? upscaler, UmbraPromptHash? promptHash)
        {
            return Upscaler == upscaler &&
                   PromptHash == promptHash &&
                   Enhancements.SequenceEqual(enhancements);
        }
    }
}
