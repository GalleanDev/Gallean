namespace Gallean.Engine.Contracts;

public sealed record WorldMountain(
    WorldMountainId Id,
    WorldFootprint Footprint,
    WorldRace Race,
    WorldObjectVariant Variant);
