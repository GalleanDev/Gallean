namespace Gallean.Engine;

internal sealed record UnitAttackTypeInfo(UnitAttackTargetType TargetType, bool CanMiss, bool IsWardable, bool TargetsDead, bool IsRecovery);
