using Gallean.Common;

namespace Gallean.Engine;

internal sealed class PoisonAttackResolver : BattleAbstractDamageOverTimeAttackResolver, IStaticInstance<PoisonAttackResolver>
{
    protected override BattleAbstractStatusEffect CreateEffect(int remainingTurns, int damage)
    {
        return new BattlePoisonStatusEffect(remainingTurns, damage);
    }

    protected override BattleAbstractDamageOverTimeEffect? FindEffect(BattleUnit target)
    {
        return target.StatusEffects.OfType<BattlePoisonStatusEffect>().FirstOrDefault();
    }
}
