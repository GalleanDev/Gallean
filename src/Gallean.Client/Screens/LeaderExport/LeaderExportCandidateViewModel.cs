namespace Gallean.Client;

internal sealed record LeaderExportCandidateViewModel(
    WorldStackId StackId,
    AssetSpriteKey.UnitPortrait PortraitKey,
    string Name,
    string LevelText,
    string BattlesWonText,
    UnitDetailsViewModel Details) : IDetailsSource
{
    object? IDetailsSource.DetailsContent => Details;
}
