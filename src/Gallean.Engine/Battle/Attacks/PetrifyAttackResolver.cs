using Gallean.Common;

namespace Gallean.Engine;

internal sealed class PetrifyAttackResolver : BattleAbstractDisableAttackResolver, IStaticInstance<PetrifyAttackResolver>
{
    protected override BattleAbstractStatusEffect CreateEffect(int remainingTurns)
    {
        return new BattlePetrifyStatusEffect(remainingTurns);
    }
}
