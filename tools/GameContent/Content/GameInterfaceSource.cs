using Gallean.Engine.Contracts;
using static GameContent.GameAssetDescriptor;
using static GameContent.GameResourceNames;

namespace GameContent;

internal sealed class GameInterfaceSource
{
    private readonly GameImages images;
    private readonly SceneInterface mainMenuScene;
    private readonly IReadOnlyList<ImageSceneElement> mainMenuAnimations;
    private readonly SceneInterface chooseRaceScene;

    public GameInterfaceSource(GameImages images, SceneInterfaceExtractor scenes)
    {
        this.images = images;
        mainMenuScene = scenes.GetSceneInterface(Interface.MainMenuScene);
        mainMenuAnimations = FindAnimatedElements(mainMenuScene);
        chooseRaceScene = scenes.GetSceneInterface(Interface.ChooseRaceScene);
    }

    public static IEnumerable<GameAssetDescriptor> ListDescriptors()
    {
        yield return RequiredSprite(
            new AssetSpriteKey.MainMenuBackground(),
            source => source.Interfaces.GetMainMenuBackground());

        for (int variant = 0; variant < AssetAnimationKey.MainMenuBackgroundOverlay.VariantCount; variant++)
        {
            AssetAnimationKey.MainMenuBackgroundOverlay key = new(variant);

            yield return RequiredAnimation(key, source => source.Interfaces.GetMainMenuAnimation(key.Variant));
        }

        foreach (WorldRace race in Enum.GetValues<WorldRace>())
        {
            if (race != WorldRace.Neutral)
            {
                yield return RequiredSprite(
                    new AssetSpriteKey.RaceSelectionBackground(race),
                    source => source.Interfaces.GetRaceSelectionBackground(race));

                for (int variant = 0; variant < AssetAnimationKey.RaceSelectionBackgroundOverlay.VariantCount; variant++)
                {
                    AssetAnimationKey.RaceSelectionBackgroundOverlay key = new(race, variant);

                    yield return OptionalAnimation(key, source => source.Interfaces.GetRaceSelectionAnimation(key.Race, key.Variant));
                }
            }
        }

        foreach (AssetAnimationKey.SiteType type in Enum.GetValues<AssetAnimationKey.SiteType>())
        {
            yield return RequiredSprite(new AssetSpriteKey.SiteImage(type), source => source.Interfaces.GetSiteImage(type));
        }
    }

    private AssetSprite? GetMainMenuBackground()
    {
        if (mainMenuScene.BackgroundImageName is null)
        {
            return null;
        }

        return images.TryGetSprite(mainMenuScene.BackgroundImageName);
    }

    private AssetAnimation? GetMainMenuAnimation(int index)
    {
        if (index >= mainMenuAnimations.Count)
        {
            return null;
        }

        ImageSceneElement element = mainMenuAnimations[index];

        return images.TryGetPositionedAnimation(
            element.ImageName!,
            mainMenuScene.Bounds.Width,
            mainMenuScene.Bounds.Height,
            element.Position.X,
            element.Position.Y);
    }

    private AssetSprite? GetRaceSelectionBackground(WorldRace race)
    {
        return images.TryGetSprite(Interface.ChooseRaceBackground(race));
    }

    private AssetSprite? GetSiteImage(AssetAnimationKey.SiteType type)
    {
        return images.TryGetSprite(Interface.SiteImage(type));
    }

    private AssetAnimation? GetRaceSelectionAnimation(WorldRace race, int index)
    {
        if (index < 0 || index >= Interface.ChooseRaceOverlays.Count)
        {
            return null;
        }

        string? suffix = Interface.ChooseRaceOverlaySuffix(race);

        if (suffix is null)
        {
            return null;
        }

        Interface.ChooseRaceOverlay overlay = Interface.ChooseRaceOverlays[index];
        string animationName = $"{overlay.BaseAnimationName}_{suffix}";

        if (images.HasAnimation(animationName))
        {
            ImageSceneElement? element = FindImageElement(chooseRaceScene, overlay.ElementName);

            if (element is null)
            {
                return null;
            }

            return images.TryGetPositionedAnimation(
                animationName,
                chooseRaceScene.Bounds.Width,
                chooseRaceScene.Bounds.Height,
                element.Position.X,
                element.Position.Y);
        }

        return null;
    }

    private static ImageSceneElement? FindImageElement(SceneInterface scene, string name)
    {
        foreach (SceneElement element in scene.Elements)
        {
            if (element is ImageSceneElement image && image.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return image;
            }
        }

        return null;
    }

    private IReadOnlyList<ImageSceneElement> FindAnimatedElements(SceneInterface scene)
    {
        List<ImageSceneElement> elements = [];

        foreach (SceneElement element in scene.Elements)
        {
            if (element is ImageSceneElement { ImageName: { } imageName } image && images.HasAnimation(imageName))
            {
                elements.Add(image);
            }
        }

        return elements;
    }
}
