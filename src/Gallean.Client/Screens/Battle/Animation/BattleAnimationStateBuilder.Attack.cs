using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class BattleAnimationStateBuilder
{
    private sealed class Attack(
        BattleRenderState.Unit actor,
        AttackOrigin origin,
        UnitDefinitionId? effectUnitId,
        TimeSpan touch,
        TimeSpan strike,
        TimeSpan? secondStrike,
        UnitAttackDefinition? definition)
    {
        public BattleRenderState.Unit Actor { get; } = actor;
        public AttackOrigin Origin { get; } = origin;
        public UnitDefinitionId? EffectUnitId { get; } = effectUnitId;
        public TimeSpan Touch { get; } = touch;
        public TimeSpan Strike { get; } = strike;
        public TimeSpan? SecondStrike { get; } = secondStrike;
        public UnitAttackDefinition? Definition { get; } = definition;
        public HashSet<BattleUnitId> StrikeTargets { get; } = [];

        public bool IsUnitAttack => Origin is AttackOrigin.Primary or AttackOrigin.Secondary;
    }
}
