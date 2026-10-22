namespace Gallean.Engine;

public sealed record WorldSpellDefinition(
    WorldSpellDefinitionId Id,
    GlobalTextId NameTextId,
    GlobalTextId DescriptionTextId,
    GlobalTextId EffectDescriptionTextId,
    WorldRace Race,
    int Level,
    WorldResourceSet CastingCost,
    WorldResourceSet PurchaseCost,
    int? AreaRadius,
    WorldAbstractSpellEffect Effect,
    WorldAiSpellType AiType);
