using Gallean.Common;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class BattleEndedDestroyStackRule(IEngineContext context) : WorldEventRule(context)
    {
        private sealed class DestroyerVisitor : IWorldFortificationVisitor<Empty, WorldUnitGroup?>, IStaticInstance<DestroyerVisitor>
        {
            public WorldUnitGroup? Visit(WorldCapital capital, Empty context)
            {
                return new WorldUnitGroup.Fortification(capital.Id);
            }

            public WorldUnitGroup? Visit(WorldCity city, Empty context)
            {
                return new WorldUnitGroup.Fortification(city.Id);
            }

            public WorldUnitGroup? Visit(WorldRuin ruin, Empty context)
            {
                return null;
            }
        }

        public override EventRuleResult<WorldEvent> Visit(WorldFortificationBattleEndedEvent ev, WorldState state)
        {
            if (ev.SurvivingAttackerUnits.HasAliveUnits())
            {
                return EventRuleResult<WorldEvent>.Empty;
            }
            else
            {
                WorldFortification fortification = state.Fortifications[ev.DefenderFortificationId];
                WorldUnitGroup? destroyer = fortification.Accept(DestroyerVisitor.Instance, Empty.Value);
                WorldPlayerId? ownerId = FortificationCalculator.GetOwner(fortification);

                return EventRuleResult<WorldEvent>.Append(new WorldStackDestroyedEvent(ev.AttackerStackId, destroyer, ownerId));
            }
        }

        public override EventRuleResult<WorldEvent> Visit(WorldStackBattleEndedEvent ev, WorldState state)
        {
            bool attackerSurvived = ev.SurvivingAttackerUnits.HasAliveUnits();
            bool defenderSurvived = ev.SurvivingDefenderUnits.HasAliveUnits();

            if (!attackerSurvived)
            {
                WorldStack? destroyer = defenderSurvived ? state.Stacks[ev.DefenderStackId] : null;
                WorldUnitGroup? destroyerGroup = destroyer is null ? null : new WorldUnitGroup.Stack(destroyer.Id);

                return EventRuleResult<WorldEvent>.Append(new WorldStackDestroyedEvent(ev.AttackerStackId, destroyerGroup, destroyer?.OwnerId));
            }
            else if (!defenderSurvived)
            {
                WorldStack destroyer = state.Stacks[ev.AttackerStackId];

                return EventRuleResult<WorldEvent>.Append(new WorldStackDestroyedEvent(ev.DefenderStackId, new WorldUnitGroup.Stack(destroyer.Id), destroyer.OwnerId));
            }
            else
            {
                return EventRuleResult<WorldEvent>.Empty;
            }
        }
    }
}
