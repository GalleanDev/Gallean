namespace Gallean.Engine;

public sealed record WorldThiefStealSpellAction(WorldPosition TargetPosition, WorldSpellDefinitionId SpellId) : WorldThiefAction;
