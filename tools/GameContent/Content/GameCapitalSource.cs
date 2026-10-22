using Gallean.Engine.Contracts;
using static GameContent.GameAssetDescriptor;

namespace GameContent;

internal sealed class GameCapitalSource(GameImages images, CapitalSceneExtractor extractor)
{
    private const int CanvasWidth = 640;
    private const int CanvasHeight = 600;

    private readonly IReadOnlyDictionary<Db.Id, CapitalSceneLayer> buildingLayers = extractor.GetAllCapitalBuildings();
    private readonly IReadOnlyDictionary<string, CapitalScene> scenes = extractor.GetAllCapitalScenes();

    public static IEnumerable<GameAssetDescriptor> ListDescriptors(IReadOnlyCollection<WorldPlayerBuildingDefinitionId> buildingIds)
    {
        foreach (WorldRace race in Enum.GetValues<WorldRace>())
        {
            if (race != WorldRace.Neutral)
            {
                AssetSpriteKey.CapitalBackground background = new(race, 0);

                yield return RequiredSprite(background, source => source.Capitals.GetBackground(background.Race, background.Variant));

                for (int variant = 1; variant < AssetSpriteKey.CapitalBackground.VariantCount; variant++)
                {
                    AssetSpriteKey.CapitalBackground key = new(race, variant);

                    yield return OptionalSprite(key, source => source.Capitals.GetBackground(key.Race, key.Variant));
                }

                for (int variant = 0; variant < AssetAnimationKey.CapitalBackgroundOverlay.VariantCount; variant++)
                {
                    AssetAnimationKey.CapitalBackgroundOverlay key = new(race, variant);

                    yield return OptionalAnimation(key, source => source.Capitals.GetSceneAnimation(key.Race, key.Variant));
                }
            }
        }

        foreach (WorldPlayerBuildingDefinitionId buildingId in buildingIds)
        {
            AssetAnimationKey.CapitalBuilding key = new(buildingId);

            yield return RequiredAnimation(key, source => source.Capitals.GetBuildingAnimation(key));
        }
    }

    public AssetSprite? GetBackground(WorldRace race, int index)
    {
        CapitalScene? scene = GetScene(race);

        if (scene is null)
        {
            return null;
        }

        int currentIndex = 0;

        foreach (CapitalSceneLayer layer in scene.Layers)
        {
            if (layer.BuildingId is not null)
            {
                continue;
            }

            AssetSprite? sprite = images.TryGetSprite(layer.ImageName);

            if (sprite is null)
            {
                continue;
            }

            if (currentIndex == index)
            {
                return sprite;
            }

            currentIndex++;
        }

        return null;
    }

    public AssetAnimation? GetBuildingAnimation(AssetAnimationKey.CapitalBuilding building)
    {
        Db.Id buildingId = new(building.BuildingId.Value);

        if (!buildingLayers.TryGetValue(buildingId, out CapitalSceneLayer? layer))
        {
            return null;
        }

        AssetAnimation? animation = images.TryGetPositionedAnimation(
            layer.ImageName,
            CanvasWidth,
            CanvasHeight,
            layer.X,
            layer.Y);

        if (animation is not null)
        {
            return animation;
        }

        AssetSprite? sprite = images.TryGetSprite(layer.ImageName);

        if (sprite is null)
        {
            return null;
        }

        AssetAnimationMetadata metadata = new(1, sprite.Metadata.Width, sprite.Metadata.Height, 0, 0, TriggerFrame: null, SecondTriggerFrame: null);
        IReadOnlyList<AssetSprite> frames = SpriteOperations.CropFramesToContent([sprite], SpriteOperations.ContentOrigin.Canvas, margin: 0);

        return new AssetAnimation(metadata, frames);
    }

    public AssetAnimation? GetSceneAnimation(WorldRace race, int index)
    {
        CapitalScene? scene = GetScene(race);

        if (scene is null)
        {
            return null;
        }

        int currentIndex = 0;

        foreach (CapitalSceneLayer layer in scene.Layers)
        {
            if (layer.BuildingId is not null)
            {
                continue;
            }

            AssetAnimation? animation = images.TryGetPositionedAnimation(
                layer.ImageName,
                CanvasWidth,
                CanvasHeight,
                layer.X,
                layer.Y);

            if (animation is null)
            {
                continue;
            }

            if (currentIndex == index)
            {
                return animation;
            }

            currentIndex++;
        }

        return null;
    }

    private CapitalScene? GetScene(WorldRace race)
    {
        return scenes.GetValueOrDefault(race.ToString());

    }
}
