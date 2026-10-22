namespace Gallean.Engine;

internal sealed partial class BattleAiPlanner
{
    private sealed record HealItemAction(IEngineContext Context, int Priority) : AbstractAction(Context, Priority)
    {
        private static readonly UtilityScore.ResponseCurve WoundedCurve =
            new UtilityScore.ResponseCurve.Polynomial(Slope: MaxScore, Exponent: 3, XShift: 0, YShift: 0);

        public override BattleAiActionPlan? Plan(BattleState state, BattleUnit unit)
        {
            (int Score, BattleUseItemCommand Command)? best = null;

            foreach (BattleItem item in state.Items)
            {
                ItemDefinition itemDefinition = Context.Definitions.GetItem(item.ItemId);

                if (item.Side == unit.Side && itemDefinition.Category == ItemCategory.PotionHeal)
                {
                    foreach ((BattleUnit target, GridPosition _) in state.GetGrid(unit.Side).GetUnits())
                    {
                        if (target.SummonerId is null)
                        {
                            BattleUseItemCommand command = new(unit.Id, item.Slot, new BattleAttackUnitTarget(target.Id));
                            BattleCommandResult commandResult = command.Execute(state, Context);

                            if (!commandResult.Failed)
                            {
                                int score = UtilityScore.Combine([Wounded(state, target)]);

                                if (score > 0 && (best is null || score > best.Value.Score))
                                {
                                    best = (score, command);
                                }
                            }
                        }
                    }
                }
            }

            if (best is null)
            {
                return null;
            }
            else
            {
                return MakePlan(best.Value.Score, best.Value.Command);
            }
        }

        private UtilityScore.Consideration Wounded(BattleState state, BattleUnit target)
        {
            int maxHitpoints = Context.UnitStats.GetBattleUnitStats(state, target.Id).MaxHitpoints;

            return UtilityScore.Ratio(maxHitpoints - target.Hitpoints, maxHitpoints, WoundedCurve);
        }
    }
}
