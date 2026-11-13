namespace Gallean.Engine.Contracts;

public sealed record UnitAttackClassWard(UnitAttackType AttackType, UnitWardCategory Category) : UnitWard(Category);
