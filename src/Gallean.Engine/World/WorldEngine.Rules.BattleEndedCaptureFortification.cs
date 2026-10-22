namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class BattleEndedCaptureFortificationRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldFortificationBattleEndedEvent ev, WorldState state)
        {
            if (ev.WinningSide != BattleSide.Attacker || !ev.SurvivingAttackerUnits.HasAliveUnits())
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            WorldStack attackerStack = state.Stacks[ev.AttackerStackId];

            if (!FortificationCalculator.CanCapture(attackerStack, Context.Definitions))
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            WorldFortification fortification = state.Fortifications[ev.DefenderFortificationId];
            WorldPlayerId? owner = FortificationCalculator.GetOwner(fortification);

            if (owner is not null)
            {
                return EventRuleResult<WorldEvent>.Append(
                    new WorldStackMovingEvent(
                        attackerStack.Id,
                        attackerStack.Position,
                        fortification.Entrance,
                        StackStepCalculator.GetDirection(attackerStack.Position, fortification.Entrance),
                        attackerStack.MovementPoints,
                        [],
                        null));
            }
            else
            {
                return EventRuleResult<WorldEvent>.Empty;
            }
        }
    }
}
