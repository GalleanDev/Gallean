namespace Gallean.Client;

internal sealed record LeaderExportCandidateViewModel(
    WorldStackId StackId,
    AssetSpriteKey.UnitPortrait PortraitKey,
    string Name,
    string Summary,
    UnitDetailsViewModel Details) : IDetailsSource
{
    object? IDetailsSource.DetailsContent => Details;
}
