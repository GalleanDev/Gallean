using Gallean.Common;

namespace Gallean.Engine;

internal sealed class ParalyzeAttackResolver : BattleAbstractDisableAttackResolver, IStaticInstance<ParalyzeAttackResolver>
{
    protected override BattleAbstractStatusEffect CreateEffect(int remainingTurns)
    {
        return new BattleParalyzeStatusEffect(remainingTurns);
    }
}
