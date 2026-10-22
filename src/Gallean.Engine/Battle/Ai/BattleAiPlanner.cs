namespace Gallean.Engine;

internal sealed partial class BattleAiPlanner(IEngineContext context, bool canUseItems)
{
    private readonly IReadOnlyList<AbstractAction> actions = CreateActions(context, canUseItems);

    public BattleAiActionPlan DecideAction(BattleState state)
    {
        BattleUnit unit = state.GetUnit(state.CurrentUnitId);

        if (!unit.IsActive())
        {
            return BattleAiActionPlan.Skip(state.CurrentUnitId);
        }

        BattleAiActionPlan? best = null;

        foreach (AbstractAction action in actions)
        {
            BattleAiActionPlan? plan = action.Plan(state, unit);

            if (plan is not null && (best is null || plan.Value.Score > best.Value.Score))
            {
                best = plan.Value;
            }
        }

        if (best is not null)
        {
            return best.Value;
        }

        return BattleAiActionPlan.Skip(state.CurrentUnitId);
    }

    private static IReadOnlyList<AbstractAction> CreateActions(IEngineContext context, bool canUseItems)
    {
        if (canUseItems)
        {
            return
            [
                new HealItemAction(context, Priority: 3),
                new AttackAction(context, Priority: 3),
                new DefendAction(context, Priority: 3),
                new SkipAction(context, Priority: 1)
            ];
        }
        else
        {
            return
            [
                new AttackAction(context, Priority: 3),
                new DefendAction(context, Priority: 3),
                new SkipAction(context, Priority: 1)
            ];
        }
    }
}
