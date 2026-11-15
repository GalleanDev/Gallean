using Gallean.Engine;

namespace Gallean.Client;

internal sealed record SpellDetailsViewModel(
    IconViewModel.Spell Icon,
    string Name,
    string LevelText,
    string Description,
    SummonViewModel? Summon,
    string EffectText,
    OfferPriceViewModel CastingCost)
{
    public static SpellDetailsViewModel Create(WorldSpellDefinitionId spellId, IGameDefinitionProvider definitions, IGameTextProvider texts)
    {
        WorldSpellDefinition definition = definitions.GetSpell(spellId);

        return new SpellDetailsViewModel(
            SpellIconResolver.Resolve(definitions, spellId),
            texts.GetGlobalText(definition.NameTextId).Text,
            Tr("Level {0}", definition.Level),
            texts.GetGlobalText(definition.DescriptionTextId).Text,
            CreateSummon(definition, definitions, texts),
            texts.GetGlobalText(definition.EffectDescriptionTextId).Text,
            new OfferPriceViewModel(definition.CastingCost));
    }

    private static SummonViewModel? CreateSummon(WorldSpellDefinition definition, IGameDefinitionProvider definitions, IGameTextProvider texts)
    {
        if (definition.Effect is WorldSummonStackSpellEffect effect)
        {
            UnitDefinition unit = definitions.GetUnit(effect.Unit);

            return SummonViewModel.Create(unit, texts);
        }
        else
        {
            return null;
        }
    }
}
