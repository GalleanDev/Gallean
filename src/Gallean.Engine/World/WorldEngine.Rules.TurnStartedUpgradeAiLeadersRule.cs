using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class TurnStartedUpgradeAiLeadersRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldTurnStartedEvent ev, WorldState state)
        {
            WorldPlayer player = state.Players[ev.PlayerId];

            if (!player.IsAi)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            List<WorldEvent> events = [];
            IEnumerable<WorldStack> upgradable = state.Stacks.Values.Where(s => s.OwnerId == ev.PlayerId && s.PendingLeaderUpgrades > 0);

            foreach (WorldStack stack in upgradable)
            {
                events.AddRange(SelectUpgrades(player, stack));
            }

            return EventRuleResult<WorldEvent>.Append([.. events]);
        }

        private IEnumerable<WorldEvent> SelectUpgrades(WorldPlayer player, WorldStack stack)
        {
            (WorldUnit leader, GridPosition _, UnitDefinition _) = stack.GetLeader(Context.Definitions);

            ImmutableArray<UnitModifier> modifiers = leader.Modifiers;

            for (int i = 0; i < stack.PendingLeaderUpgrades; i++)
            {
                IReadOnlyList<LeaderUpgradeDefinition> available = LeaderUpgradeCalculator.GetAvailableUpgrades(
                    player,
                    leader.Level,
                    modifiers,
                    Context.Definitions);

                if (LeaderUpgradeCalculator.GetPreferredUpgrade(available) is { } upgrade)
                {
                    modifiers = modifiers.Add(UnitModifier.Permanent(upgrade.ModifierId));

                    yield return new WorldLeaderUpgradeSelectedEvent(stack.Id, upgrade.ModifierId);

                    yield return new WorldUnitModifierAppliedEvent(
                        new WorldUnitGroup.Stack(stack.Id),
                        leader.Id,
                        UnitModifier.Permanent(upgrade.ModifierId));
                }
                else
                {
                    yield break;
                }
            }
        }
    }
}
