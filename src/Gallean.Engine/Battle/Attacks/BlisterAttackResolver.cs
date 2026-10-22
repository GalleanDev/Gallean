using Gallean.Common;

namespace Gallean.Engine;

internal sealed class BlisterAttackResolver : BattleAbstractDamageOverTimeAttackResolver, IStaticInstance<BlisterAttackResolver>
{
    protected override BattleAbstractStatusEffect CreateEffect(int remainingTurns, int damage)
    {
        return new BattleBlisterStatusEffect(remainingTurns, damage);
    }

    protected override BattleAbstractDamageOverTimeEffect? FindEffect(BattleUnit target)
    {
        return target.StatusEffects.OfType<BattleBlisterStatusEffect>().FirstOrDefault();
    }
}
