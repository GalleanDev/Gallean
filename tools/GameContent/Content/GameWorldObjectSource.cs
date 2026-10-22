using Gallean.Engine.Contracts;
using static GameContent.GameAssetDescriptor;
using static GameContent.GameResourceNames;

namespace GameContent;

internal sealed class GameWorldObjectSource(GameImages still, GameImages common, GameImages animations)
{
    public static IEnumerable<GameAssetDescriptor> ListDescriptors(IReadOnlyCollection<WorldLandmarkDefinitionId> landmarkIds)
    {
        foreach (WorldResourceType resourceType in Enum.GetValues<WorldResourceType>())
        {
            AssetAnimationKey.Crystal key = new(resourceType);

            yield return RequiredAnimation(key, source => AnimationOperations.TryCreate(source.WorldObjects.GetCrystalFrames(key.ResourceType)));
        }

        for (int level = AssetAnimationKey.City.MinLevel; level <= AssetAnimationKey.City.MaxLevel; level++)
        {
            AssetAnimationKey.City key = new(level);

            yield return RequiredAnimation(key, source => AnimationOperations.TryCreate(source.WorldObjects.GetCityFrames(key.Level)));
        }

        for (int level = AssetAnimationKey.City.MinLevel; level <= AssetAnimationKey.City.MaxLevel; level++)
        {
            AssetAnimationKey.CityShadow key = new(level);

            yield return OptionalAnimation(key, source => AnimationOperations.TryCreate(source.WorldObjects.GetCityShadowFrames(key.Level)));
        }

        foreach (WorldRace race in Enum.GetValues<WorldRace>())
        {
            AssetAnimationKey.Capital intact = new(race, IsCaptured: false);
            AssetAnimationKey.Capital captured = new(race, IsCaptured: true);

            yield return RequiredAnimation(intact, source => AnimationOperations.TryCreate(source.WorldObjects.GetCapitalFrames(intact.Race, intact.IsCaptured)));
            yield return RequiredAnimation(captured, source => AnimationOperations.TryCreate(source.WorldObjects.GetCapitalFrames(captured.Race, captured.IsCaptured)));
        }

        foreach (WorldRace race in Enum.GetValues<WorldRace>())
        {
            yield return RequiredSprite(
                new AssetSpriteKey.CityShield(race, IsCapital: false),
                source => source.WorldObjects.GetCityShield(race, isCapital: false));

            if (race != WorldRace.Neutral)
            {
                yield return RequiredSprite(
                    new AssetSpriteKey.CityShield(race, IsCapital: true),
                    source => source.WorldObjects.GetCityShield(race, isCapital: true));
            }
        }

        foreach (WorldRace race in Enum.GetValues<WorldRace>())
        {
            if (race != WorldRace.Neutral)
            {
                yield return RequiredSprite(new AssetSpriteKey.Rod(race), source => source.WorldObjects.GetRod(race));
            }
        }

        for (int variant = 0; variant < AssetAnimationKey.Ruin.VariantCount; variant++)
        {
            AssetAnimationKey.Ruin intact = new(IsLooted: false, variant);
            AssetAnimationKey.Ruin looted = new(IsLooted: true, variant);

            yield return RequiredAnimation(intact, source => AnimationOperations.TryCreate(source.WorldObjects.GetRuinFrames(intact.IsLooted, intact.Variant)));
            yield return RequiredAnimation(looted, source => AnimationOperations.TryCreate(source.WorldObjects.GetRuinFrames(looted.IsLooted, looted.Variant)));
        }

        for (int variant = 0; variant < AssetSpriteKey.Bag.GetVariantCount(isWater: false); variant++)
        {
            AssetSpriteKey.Bag key = new(IsWater: false, variant);

            yield return RequiredSprite(key, source => source.WorldObjects.GetBag(key.IsWater, key.Variant));
        }

        for (int variant = 0; variant < AssetSpriteKey.Bag.GetVariantCount(isWater: true); variant++)
        {
            AssetSpriteKey.Bag key = new(IsWater: true, variant);

            yield return RequiredSprite(key, source => source.WorldObjects.GetBag(key.IsWater, key.Variant));
        }

        yield return RequiredSprite(
            new AssetSpriteKey.Tombstone(IsWater: false),
            source => source.WorldObjects.GetTombstone(isWater: false));

        yield return RequiredSprite(
            new AssetSpriteKey.Tombstone(IsWater: true),
            source => source.WorldObjects.GetTombstone(isWater: true));

        for (int variant = 0; variant < AssetSpriteKey.StackBanner.VariantCount; variant++)
        {
            AssetSpriteKey.StackBanner key = new(variant);

            yield return RequiredSprite(key, source => source.WorldObjects.GetBanner(key.Variant));
        }

        yield return RequiredSprite(
            new AssetSpriteKey.RuinBanner(IsLooted: false),
            source => source.WorldObjects.GetRuinBanner(isLooted: false));

        yield return RequiredSprite(
            new AssetSpriteKey.RuinBanner(IsLooted: true),
            source => source.WorldObjects.GetRuinBanner(isLooted: true));

        foreach (AssetAnimationKey.SiteType type in Enum.GetValues<AssetAnimationKey.SiteType>())
        {
            yield return RequiredAnimation(
                new AssetAnimationKey.SiteBanner(type),
                source => AnimationOperations.TryCreate(source.WorldObjects.GetSiteBannerFrames(type)));
        }

        foreach (AssetAnimationKey.SiteType type in Enum.GetValues<AssetAnimationKey.SiteType>())
        {
            for (int variant = 0; variant < AssetAnimationKey.Site.GetVariantCount(type); variant++)
            {
                AssetAnimationKey.Site key = new(type, variant);

                yield return RequiredAnimation(key, source => AnimationOperations.TryCreate(source.WorldObjects.GetSiteFrames(key.Type, key.Variant)));
            }
        }

        foreach (AssetAnimationKey.SiteType type in Enum.GetValues<AssetAnimationKey.SiteType>())
        {
            for (int variant = 0; variant < AssetAnimationKey.Site.GetVariantCount(type); variant++)
            {
                AssetAnimationKey.SiteShadow key = new(type, variant);

                yield return OptionalAnimation(key, source => AnimationOperations.TryCreate(source.WorldObjects.GetSiteShadowFrames(key.Type, key.Variant)));
            }
        }

        for (int variant = 0; variant < AssetAnimationKey.Cloud.VariantCount; variant++)
        {
            AssetAnimationKey.Cloud key = new(variant);

            yield return RequiredAnimation(key, source => AnimationOperations.TryCreate(source.WorldObjects.GetCloudFrames(key.Variant)));
        }

        foreach (WorldLandmarkDefinitionId landmarkId in landmarkIds)
        {
            AssetAnimationKey.Landmark key = new(landmarkId);

            yield return RequiredAnimation(key, source => AnimationOperations.TryCreate(source.WorldObjects.GetLandmarkFrames(key.LandmarkId)));
        }

        foreach (WorldLandmarkDefinitionId landmarkId in landmarkIds)
        {
            AssetAnimationKey.WaterLandmark key = new(landmarkId);

            yield return RequiredAnimation(key, source => AnimationOperations.TryCreate(source.WorldObjects.GetLandmarkFrames(key.LandmarkId)));
        }
    }

    private IReadOnlyList<AssetSprite>? GetCrystalFrames(WorldResourceType resourceType)
    {
        return GetIsoObjectFrames(WorldObjects.Crystal(resourceType));
    }

    private IReadOnlyList<AssetSprite>? GetCityFrames(int level)
    {
        return GetIsoObjectFrames(WorldObjects.City(level));
    }

    private IReadOnlyList<AssetSprite>? GetCityShadowFrames(int level)
    {
        return GetIsoObjectFrames(WorldObjects.CityShadow(level));
    }

    private IReadOnlyList<AssetSprite>? GetCapitalFrames(WorldRace race, bool isCaptured)
    {
        return GetIsoObjectFrames(WorldObjects.Capital(race, isCaptured));
    }

    private AssetSprite? GetCityShield(WorldRace race, bool isCapital)
    {
        return GetIsoObjectSprite(WorldObjects.CityShield(race, isCapital));
    }

    private AssetSprite? GetRod(WorldRace race)
    {
        return GetIsoObjectSprite(WorldObjects.Rod(race));
    }

    private IReadOnlyList<AssetSprite>? GetRuinFrames(bool isLooted, int variant)
    {
        return GetIsoObjectFrames(WorldObjects.Ruin(isLooted, variant));
    }

    private AssetSprite? GetBag(bool isWater, int variant)
    {
        return common.TryGetAnchoredSprite(WorldObjects.Bag(isWater, variant));
    }

    private AssetSprite? GetTombstone(bool isWater)
    {
        return common.TryGetAnchoredSprite(WorldObjects.Tombstone(isWater));
    }

    private AssetSprite? GetBanner(int variant)
    {
        return GetIsoObjectSprite(WorldObjects.StackBanner(variant));
    }

    private AssetSprite? GetRuinBanner(bool isLooted)
    {
        return GetIsoObjectSprite(WorldObjects.RuinBanner(isLooted));
    }

    private IReadOnlyList<AssetSprite>? GetSiteBannerFrames(AssetAnimationKey.SiteType type)
    {
        return GetIsoObjectFrames(WorldObjects.SiteBanner(type));
    }

    private IReadOnlyList<AssetSprite>? GetLandmarkFrames(WorldLandmarkDefinitionId landmarkId)
    {
        return GetIsoObjectFrames(landmarkId.Value);
    }

    private IReadOnlyList<AssetSprite>? GetSiteFrames(AssetAnimationKey.SiteType type, int variant)
    {
        return GetIsoObjectFrames(WorldObjects.Site(type, variant));
    }

    private IReadOnlyList<AssetSprite>? GetSiteShadowFrames(AssetAnimationKey.SiteType type, int variant)
    {
        return GetIsoObjectFrames(WorldObjects.SiteShadow(type, variant));
    }

    private IReadOnlyList<AssetSprite>? GetCloudFrames(int variant)
    {
        return common.TryGetAnchoredAnimationFrames(WorldObjects.Cloud(variant));
    }

    private AssetSprite? GetIsoObjectSprite(string name)
    {
        IReadOnlyList<AssetSprite>? frames = GetIsoObjectFrames(name);

        return frames is { Count: > 0 } ? frames[0] : null;
    }

    private IReadOnlyList<AssetSprite>? GetIsoObjectFrames(string name)
    {
        IReadOnlyList<AssetSprite>? animated =
            animations.TryGetAnchoredAnimationFrames(name) ??
            common.TryGetAnchoredAnimationFrames(name) ??
            still.TryGetAnchoredAnimationFrames(name);

        if (animated is not null)
        {
            return animated;
        }

        AssetSprite? single = still.TryGetAnchoredSprite(name) ?? common.TryGetAnchoredSprite(name);

        if (single is not null)
        {
            return [single];
        }

        return null;
    }
}
