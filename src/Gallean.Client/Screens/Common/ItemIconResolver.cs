using Gallean.Engine;

namespace Gallean.Client;

internal static class ItemIconResolver
{
    public static AssetSpriteKey.ItemIcon Resolve(IGameDefinitionProvider definitions, ItemDefinitionId definitionId)
    {
        ItemDefinition definition = definitions.GetItem(definitionId);
        AssetSpriteKey.ItemIconSource source;

        if (definition is { Category: ItemCategory.Scroll, Effect: SpellItemEffect effect })
        {
            WorldSpellDefinition spellDefinition = definitions.GetSpell(effect.SpellId);

            source = new AssetSpriteKey.ItemIconSource.Scroll(spellDefinition.Race);
        }
        else
        {
            source = new AssetSpriteKey.ItemIconSource.Item(definitionId);
        }

        return new AssetSpriteKey.ItemIcon(source);
    }
}
