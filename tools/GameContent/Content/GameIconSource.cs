using Gallean.Engine.Contracts;
using static GameContent.GameAssetDescriptor;
using static GameContent.GameResourceNames;

namespace GameContent;

internal sealed class GameIconSource(
    GameImages iconSpells,
    GameImages iconBuildings,
    GameImages iconItems)
{
    private static readonly IReadOnlyList<WorldRace> ScrollRaces =
    [
        WorldRace.Human,
        WorldRace.Undead,
        WorldRace.Heretic,
        WorldRace.Dwarf,
        WorldRace.Elf
    ];

    public static IEnumerable<GameAssetDescriptor> ListDescriptors(
        IReadOnlyCollection<WorldSpellDefinitionId> spellIds,
        IReadOnlyCollection<WorldPlayerBuildingDefinitionId> buildingIds,
        IReadOnlyCollection<ItemDefinitionId> itemIds)
    {
        foreach (WorldSpellDefinitionId spellId in spellIds)
        {
            AssetSpriteKey.SpellIcon key = new(spellId);

            yield return RequiredSprite(key, source => source.Icons.GetSpellIcon(key.SpellId));
        }

        foreach (WorldPlayerBuildingDefinitionId buildingId in buildingIds)
        {
            AssetSpriteKey.BuildingIcon key = new(buildingId);

            yield return RequiredSprite(key, source => source.Icons.GetBuildingIcon(key.BuildingId));
        }

        foreach (ItemDefinitionId itemId in itemIds)
        {
            AssetSpriteKey.ItemIcon key = new(new AssetSpriteKey.ItemIconSource.Item(itemId));

            yield return OptionalSprite(key, source => source.Icons.GetItemIcon(key.Source));
        }

        foreach (WorldRace race in ScrollRaces)
        {
            AssetSpriteKey.ItemIcon key = new(new AssetSpriteKey.ItemIconSource.Scroll(race));

            yield return RequiredSprite(key, source => source.Icons.GetItemIcon(key.Source));
        }
    }

    private AssetSprite? GetSpellIcon(WorldSpellDefinitionId spellId)
    {
        return iconSpells.TryGetSprite(spellId.Value);
    }

    private AssetSprite? GetBuildingIcon(WorldPlayerBuildingDefinitionId buildingId)
    {
        return iconBuildings.TryGetSprite(buildingId.Value);
    }

    private AssetSprite? GetItemIcon(AssetSpriteKey.ItemIconSource source)
    {
        return source switch
        {
            AssetSpriteKey.ItemIconSource.Item item => iconItems.TryGetSprite(item.ItemId.Value),
            AssetSpriteKey.ItemIconSource.Scroll scroll => iconItems.TryGetSprite(Icons.Scroll(scroll.Race)),
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }
}
