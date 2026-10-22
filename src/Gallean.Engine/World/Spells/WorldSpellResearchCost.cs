namespace Gallean.Engine;

public sealed record WorldSpellResearchCost(
    WorldRace Race,
    WorldSpellDefinitionId SpellId,
    WorldResourceSet Cost);
