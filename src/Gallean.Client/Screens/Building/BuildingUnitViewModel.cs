namespace Gallean.Client;

internal sealed record BuildingUnitViewModel(AssetSpriteKey.UnitFace FaceKey, AssetSpriteKey.UnitPortrait PortraitKey, string Name, UnitDetailsViewModel Details) : IDetailsSource
{
    object? IDetailsSource.DetailsContent => Details;
}
