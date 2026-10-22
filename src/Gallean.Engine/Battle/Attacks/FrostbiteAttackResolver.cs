using Gallean.Common;

namespace Gallean.Engine;

internal sealed class FrostbiteAttackResolver : BattleAbstractDamageOverTimeAttackResolver, IStaticInstance<FrostbiteAttackResolver>
{
    protected override BattleAbstractStatusEffect CreateEffect(int remainingTurns, int damage)
    {
        return new BattleFrostbiteStatusEffect(remainingTurns, damage);
    }

    protected override BattleAbstractDamageOverTimeEffect? FindEffect(BattleUnit target)
    {
        return target.StatusEffects.OfType<BattleFrostbiteStatusEffect>().FirstOrDefault();
    }
}
