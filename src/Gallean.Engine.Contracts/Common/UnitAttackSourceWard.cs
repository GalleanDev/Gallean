namespace Gallean.Engine.Contracts;

public sealed record UnitAttackSourceWard(UnitAttackSource Source, UnitWardCategory Category) : UnitWard(Category);
