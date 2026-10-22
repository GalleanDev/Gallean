namespace Gallean.Client;

internal sealed partial record WorldRenderState
{
    public abstract record MapObject(
        WorldPosition Position,
        int Depth);

    public sealed record MapTileFeature(
        WorldPosition Position,
        int Depth,
        AssetSpriteKey AssetKey) : MapObject(Position, Depth);

    public sealed record ResourceNode(
        WorldPosition Position,
        int Depth,
        AssetAnimationKey AssetKey) : MapObject(Position, Depth);

    public sealed record Fortification(
        WorldPosition Position,
        int Depth,
        WorldFootprint Footprint,
        AssetAnimationKey AssetKey) : MapObject(Position, Depth);

    public sealed record Site(
        WorldPosition Position,
        int Depth,
        WorldFootprint Footprint,
        AssetAnimationKey AssetKey) : MapObject(Position, Depth);

    public sealed record Landmark(
        WorldPosition Position,
        int Depth,
        WorldFootprint Footprint,
        AssetAnimationKey AssetKey) : MapObject(Position, Depth);

    public sealed record ObjectShadow(
        WorldPosition Position,
        int Depth,
        WorldFootprint Footprint,
        AssetAnimationKey AssetKey) : MapObject(Position, Depth);

    public sealed record Mountain(
        WorldPosition Position,
        int Depth,
        WorldFootprint Footprint,
        AssetSpriteKey AssetKey) : MapObject(Position, Depth);

    public sealed record LocationBanner(
        WorldPosition Position,
        int Depth,
        WorldFootprint Footprint,
        AssetAnimationKey AssetKey) : MapObject(Position, Depth);

    public sealed record RuinBanner(
        WorldPosition Position,
        int Depth,
        WorldFootprint Footprint,
        AssetSpriteKey AssetKey) : MapObject(Position, Depth);

    public sealed record CityShield(
        WorldPosition Position,
        int Depth,
        WorldFootprint Footprint,
        AssetSpriteKey AssetKey) : MapObject(Position, Depth);

    public sealed record Bag(
        WorldPosition Position,
        int Depth,
        AssetSpriteKey AssetKey) : MapObject(Position, Depth);

    public sealed record Rod(
        WorldPosition Position,
        int Depth,
        AssetSpriteKey AssetKey) : MapObject(Position, Depth);

    public sealed record Tomb(
        WorldPosition Position,
        int Depth,
        AssetSpriteKey AssetKey) : MapObject(Position, Depth);

    public sealed record Stack(
        WorldPosition Position,
        int Depth,
        AssetAnimationKey AssetKey,
        AssetAnimationKey? ShadowKey,
        AssetAnimationKey? AuraKey,
        AssetSpriteKey BannerKey,
        double Opacity) : MapObject(Position, Depth);
}
