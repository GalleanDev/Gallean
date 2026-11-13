namespace Gallean.Engine.Contracts;

public sealed record BattleReduceInitiativeStatusEffect(int Percent, bool IsInfinite) : BattleAbstractStatModifierEffect(Percent, IsInfinite);
