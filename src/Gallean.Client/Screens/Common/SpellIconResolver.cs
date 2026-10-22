using Gallean.Engine;

namespace Gallean.Client;

internal static class SpellIconResolver
{
    public static IconViewModel.Spell Resolve(IGameDefinitionProvider definitions, WorldSpellDefinitionId spellId)
    {
        WorldSpellDefinition definition = definitions.GetSpell(spellId);

        IconViewModel.Spell.BackgroundType background = definition.Race switch
        {
            WorldRace.Human => IconViewModel.Spell.BackgroundType.Light,
            WorldRace.Undead => IconViewModel.Spell.BackgroundType.Light,
            WorldRace.Heretic => IconViewModel.Spell.BackgroundType.Light,
            WorldRace.Dwarf => IconViewModel.Spell.BackgroundType.Dark,
            WorldRace.Elf => IconViewModel.Spell.BackgroundType.Light,
            WorldRace.Neutral => IconViewModel.Spell.BackgroundType.Light
        };

        return new IconViewModel.Spell(new AssetSpriteKey.SpellIcon(spellId), background);
    }
}
