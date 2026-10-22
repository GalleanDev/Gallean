namespace Gallean.Client;

internal sealed record MenuBackgroundViewModel(
    AssetSpriteKey SpriteKey,
    IReadOnlyList<AssetAnimationKey> OverlayKeys)
{
    public static MenuBackgroundViewModel? TryCreateRaceSelection(WorldRace race)
    {
        if (race == WorldRace.Neutral)
        {
            return null;
        }
        else
        {
            return CreateRaceSelection(race);
        }
    }

    public IEnumerable<AssetPreloadItem> GetPreloadItems()
    {
        yield return new AssetPreloadItem.Sprite(SpriteKey);

        foreach (AssetAnimationKey overlayKey in OverlayKeys)
        {
            yield return new AssetPreloadItem.Animation(overlayKey);
        }
    }

    private static MenuBackgroundViewModel CreateRaceSelection(WorldRace race)
    {
        List<AssetAnimationKey> overlayKeys = [];

        for (int variant = 0; variant < AssetAnimationKey.RaceSelectionBackgroundOverlay.VariantCount; variant++)
        {
            overlayKeys.Add(new AssetAnimationKey.RaceSelectionBackgroundOverlay(race, variant));
        }

        return new MenuBackgroundViewModel(new AssetSpriteKey.RaceSelectionBackground(race), overlayKeys);
    }
}
