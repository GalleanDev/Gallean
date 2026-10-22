using System.Diagnostics.CodeAnalysis;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class DiplomacyRelationRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldGoldGivenEvent ev, WorldState state)
        {
            int change = DiplomacyCalculator.GetGiftRelationChange(ev.Gold, Context.Globals);

            return UpdateRelation(state, ev.PlayerId, ev.TargetPlayerId, change);
        }

        public override EventRuleResult<WorldEvent> Visit(WorldStackBattleStartedEvent ev, WorldState state)
        {
            WorldStack attacker = state.Stacks[ev.AttackerStackId];
            WorldStack defender = state.Stacks[ev.DefenderStackId];

            return Attack(state, attacker.OwnerId, defender.OwnerId, Context.Globals.DiplomacyAttackStackRelationChange);
        }

        public override EventRuleResult<WorldEvent> Visit(WorldFortificationBattleStartedEvent ev, WorldState state)
        {
            WorldStack attacker = state.Stacks[ev.AttackerStackId];
            WorldFortification defender = state.Fortifications[ev.DefenderFortificationId];
            WorldPlayerId? defenderOwnerId = FortificationCalculator.GetOwner(defender);

            if (defenderOwnerId is not null)
            {
                return Attack(state, attacker.OwnerId, defenderOwnerId.Value, Context.Globals.DiplomacyAttackFortificationRelationChange);
            }
            else
            {
                return EventRuleResult<WorldEvent>.Empty;
            }
        }

        public override EventRuleResult<WorldEvent> Visit(WorldThiefActionSucceededEvent ev, WorldState state)
        {
            if (ev.TargetPlayerId is null)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            WorldPlayerId targetPlayerId = ev.TargetPlayerId.Value;

            if (state.Players[targetPlayerId].Race == WorldRace.Neutral)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            WorldStack thief = state.Stacks[ev.ThiefStackId];

            return UpdateRelation(state, thief.OwnerId, targetPlayerId, Context.Globals.DiplomacyThiefActionRelationChange);
        }

        public override EventRuleResult<WorldEvent> Visit(WorldRodDestroyedEvent ev, WorldState state)
        {
            return UpdateRelation(state, ev.PlayerId, ev.TargetPlayerId, Context.Globals.DiplomacyDestroyRodRelationChange);
        }

        public override EventRuleResult<WorldEvent> Visit(WorldAllianceEndedEvent ev, WorldState state)
        {
            return SetRelation(state, ev.PlayerId, ev.TargetPlayerId, Context.Globals.DiplomacyBreakAllianceRelation);
        }

        public override EventRuleResult<WorldEvent> Visit(WorldDiplomacyProposalAnsweredEvent ev, WorldState state)
        {
            if (ev.Proposal.Action is not WorldDiplomacyProposeAllianceAction)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            WorldPlayerId playerId = ev.Proposal.PlayerId;
            WorldPlayerId targetPlayerId = ev.Proposal.TargetPlayerId;
            bool bothHuman = !state.Players[playerId].IsAi && !state.Players[targetPlayerId].IsAi;

            if (ev.Accepted)
            {
                if (bothHuman)
                {
                    WorldRelation relation = DiplomacyCalculator.GetRelation(state, playerId, targetPlayerId);

                    return SetRelation(state, playerId, targetPlayerId, Math.Max(relation.Current, Context.Globals.DiplomacyNeutralRelation + 1));
                }
                else
                {
                    return SetRelation(state, playerId, targetPlayerId, Context.Globals.DiplomacyMakeAllianceRelation);
                }
            }
            else
            {
                if (bothHuman)
                {
                    return EventRuleResult<WorldEvent>.Empty;
                }
                else
                {
                    return SetRelation(state, playerId, targetPlayerId, Context.Globals.DiplomacyRefuseAllianceRelation);
                }
            }
        }

        public override EventRuleResult<WorldEvent> Visit(WorldDiplomacyItemTradedEvent ev, WorldState state)
        {
            int value = DiplomacyProposalCalculator.GetTradeValue(ev.Item.DefinitionId, Context);

            return UpdateTradeRelation(state, ev.SellerId, ev.BuyerId, value, ev.Price);
        }

        public override EventRuleResult<WorldEvent> Visit(WorldDiplomacySpellTradedEvent ev, WorldState state)
        {
            int value = DiplomacyProposalCalculator.GetTradeValue(ev.SpellId, Context);

            return UpdateTradeRelation(state, ev.SellerId, ev.BuyerId, value, ev.Price);
        }

        private EventRuleResult<WorldEvent> UpdateTradeRelation(
            WorldState state,
            WorldPlayerId sellerId,
            WorldPlayerId buyerId,
            int value,
            int price)
        {
            int reward = Math.Max(1, value - price) / Context.Globals.DiplomacyGiftGoldPerRelationChange;

            return UpdateRelation(state, sellerId, buyerId, reward);
        }

        private EventRuleResult<WorldEvent> Attack(
            WorldState state,
            WorldPlayerId playerId,
            WorldPlayerId targetPlayerId,
            int relationChange)
        {
            if (state.Players[targetPlayerId].Race == WorldRace.Neutral)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            WorldRelation relation = DiplomacyCalculator.GetRelation(state, playerId, targetPlayerId);
            WorldRelationLevel level = DiplomacyCalculator.ResolveRelationLevel(relation.Current, Context.Globals);

            if (level == WorldRelationLevel.Peace)
            {
                return SetRelation(state, playerId, targetPlayerId, Context.Globals.DiplomacyAttackAtPeaceRelation);
            }
            else
            {
                return UpdateRelation(state, playerId, targetPlayerId, relationChange);
            }
        }

        private EventRuleResult<WorldEvent> UpdateRelation(
            WorldState state,
            WorldPlayerId playerId,
            WorldPlayerId targetPlayerId,
            int delta)
        {
            WorldRelation relation = DiplomacyCalculator.GetRelation(state, playerId, targetPlayerId);

            return EmitRelationChanges(state, playerId, targetPlayerId, relation.Current + delta, -delta / 2);
        }

        private EventRuleResult<WorldEvent> SetRelation(
            WorldState state,
            WorldPlayerId playerId,
            WorldPlayerId targetPlayerId,
            int newValue)
        {
            return EmitRelationChanges(state, playerId, targetPlayerId, newValue, -newValue / 2);
        }

        private EventRuleResult<WorldEvent> EmitRelationChanges(
            WorldState state,
            WorldPlayerId playerId,
            WorldPlayerId targetPlayerId,
            int newValue,
            int enemyChange)
        {
            List<WorldEvent> events = [];

            if (TryCreateRelationChangedEvent(state, playerId, targetPlayerId, newValue, out WorldRelationChangedEvent? targetEvent))
            {
                events.Add(targetEvent);
            }

            foreach (WorldPlayer enemy in state.Players.Values)
            {
                if (enemy.Id != playerId && enemy.Id != targetPlayerId)
                {
                    WorldRelation targetRelation = DiplomacyCalculator.GetRelation(state, enemy.Id, targetPlayerId);
                    WorldRelationLevel targetLevel = DiplomacyCalculator.ResolveRelationLevel(targetRelation.Current, Context.Globals);

                    if (!targetRelation.PermanentWar && targetLevel == WorldRelationLevel.War)
                    {
                        WorldRelation enemyRelation = DiplomacyCalculator.GetRelation(state, playerId, enemy.Id);

                        if (TryCreateRelationChangedEvent(
                            state,
                            playerId,
                            enemy.Id,
                            enemyRelation.Current + enemyChange,
                            out WorldRelationChangedEvent? enemyEvent))
                        {
                            events.Add(enemyEvent);
                        }
                    }
                }
            }

            return EventRuleResult<WorldEvent>.Append([.. events]);
        }

        private static bool TryCreateRelationChangedEvent(
            WorldState state,
            WorldPlayerId playerId,
            WorldPlayerId otherPlayerId,
            int newValue,
            [NotNullWhen(true)] out WorldRelationChangedEvent? ev)
        {
            newValue = Math.Clamp(newValue, DiplomacyCalculator.RelationMinimum, DiplomacyCalculator.RelationMaximum);

            WorldRelation oldRelation = DiplomacyCalculator.GetRelation(state, playerId, otherPlayerId);

            if (state.Relations.ContainsKey(oldRelation.Id) &&
                !oldRelation.PermanentWar &&
                newValue != oldRelation.Current)
            {
                ev = new WorldRelationChangedEvent(playerId, otherPlayerId, newValue);
                return true;
            }
            else
            {
                ev = null;
                return false;
            }
        }
    }
}
