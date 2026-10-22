using Gallean.Common;

namespace Gallean.Engine;

public readonly record struct BattleAiActionPlan(BattleAiActionScore Score, IBattleCommand<Empty> Command)
{
    public static BattleAiActionPlan Skip(BattleUnitId unitId)
    {
        return new BattleAiActionPlan(default, new BattleSkipCommand(unitId));
    }
}
