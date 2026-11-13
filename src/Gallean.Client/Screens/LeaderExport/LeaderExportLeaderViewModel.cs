namespace Gallean.Client;

internal sealed record LeaderExportLeaderViewModel(
    WorldStackId StackId,
    AssetSpriteKey.UnitPortrait PortraitKey,
    string Name,
    string LevelText,
    string BattlesWonText,
    IReadOnlyList<AssetSpriteKey.ItemIcon> ItemIcons,
    UnitDetailsViewModel Details) : IDetailsSource
{
    object? IDetailsSource.DetailsContent => Details;
}
