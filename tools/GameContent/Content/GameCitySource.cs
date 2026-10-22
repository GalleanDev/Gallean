using Gallean.Engine.Contracts;
using static GameContent.GameAssetDescriptor;
using static GameContent.GameResourceNames;

namespace GameContent;

internal sealed class GameCitySource(GameImages images)
{
    public static IEnumerable<GameAssetDescriptor> ListDescriptors()
    {
        for (int level = AssetAnimationKey.City.MinLevel; level <= AssetAnimationKey.City.MaxLevel; level++)
        {
            AssetSpriteKey.CityImage key = new(level);

            yield return RequiredSprite(key, source => source.Cities.GetCityImage(key.Level));
        }

        foreach (WorldRace race in Enum.GetValues<WorldRace>())
        {
            if (race != WorldRace.Neutral)
            {
                AssetSpriteKey.CapitalImage key = new(race);

                yield return RequiredSprite(key, source => source.Cities.GetCapitalImage(key.Race));
            }
        }
    }

    private AssetSprite? GetCityImage(int level)
    {
        return images.TryGetSprite(Cities.CityImage(level));
    }

    private AssetSprite? GetCapitalImage(WorldRace race)
    {
        string? name = Cities.CapitalImage(race);

        if (name is null)
        {
            return null;
        }

        return images.TryGetSprite(name);
    }
}
