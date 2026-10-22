namespace Packer;

internal sealed partial class PackEnhancer
{
    private sealed record SelectedEntry(PackImageEntry Entry, IReadOnlyList<EnhancementType> Enhancements);
}
