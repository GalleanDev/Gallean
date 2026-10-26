using Gallean.Engine;

namespace Gallean.Client;

internal sealed record SpellDetailsViewModel(
    IconViewModel.Spell Icon,
    string Name,
    string LevelText,
    string Description,
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
            texts.GetGlobalText(definition.EffectDescriptionTextId).Text,
            new OfferPriceViewModel(definition.CastingCost));
    }
}
