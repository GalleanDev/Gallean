namespace Gallean.Engine;

public sealed record WorldThiefStealItemAction(WorldPosition TargetPosition, ItemDefinitionId ItemId) : WorldThiefAction;
