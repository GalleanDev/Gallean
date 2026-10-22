namespace Gallean.Client;

internal sealed record BuildingUnitViewModel(AssetSpriteKey.UnitFace FaceKey, string Name, UnitDetailsViewModel Details) : IDetailsSource
{
    object? IDetailsSource.DetailsContent => Details;
}
