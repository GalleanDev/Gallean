namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed partial record PurchaseLeaderAction(WorldAiContext Context, int Priority) : AbstractEconomyAction(Context, Priority)
    {
        private enum LeaderRole
        {
            Fighter,
            Explorer,
            Rod,
            Thief
        }

        protected override WorldResourceSet OvercommitLimit => new(400, 0, 0, 0, 0, 0);

        public override WorldAiActionPlan? Plan(WorldState state, WorldPlayerId playerId, WorldAiMemory memory)
        {
            WorldPlan plan = WorldPlan.For(state);
            WorldCapital? capital = plan.GetCapital(playerId);

            if (capital is null)
            {
                return null;
            }

            WorldPlayer player = state.Players[playerId];
            IReadOnlyList<(UnitDefinition Leader, int OwnedStacks)> wanted = [.. EnumerateWantedLeaders(state, playerId)];

            foreach ((UnitDefinition leader, int ownedStacks) in wanted)
            {
                WorldPurchaseLeaderCommand command = new(capital.Id, leader.Id);
                int score = ScorePurchase(leader, player, ownedStacks);

                if (score > 0 && !command.Execute(state, Context.Engine).Failed)
                {
                    return MakePlan(score, command, memory);
                }
            }

            foreach ((UnitDefinition leader, int ownedStacks) in wanted)
            {
                WorldPurchaseLeaderCommand command = new(capital.Id, leader.Id);
                WorldCommandResult commandResult = command.Execute(state, Context.Engine);
                int score = ScorePurchase(leader, player, ownedStacks);

                if (score > 0 &&
                    commandResult.Error is WorldCommandError.InsufficientResources &&
                    CanOvercommit(player, leader.PurchaseCost))
                {
                    return MakePlan(score, command, memory);
                }
            }

            return null;
        }

        private static int ScorePurchase(UnitDefinition leader, WorldPlayer player, int ownedStacks)
        {
            List<UtilityScore.Consideration> considerations =
            [
                Considerations.Affordability(leader, player),
                Considerations.Repetition(leader, player, ownedStacks)
            ];

            return UtilityScore.Combine(considerations);
        }

        private IEnumerable<(UnitDefinition Leader, int OwnedStacks)> EnumerateWantedLeaders(WorldState state, WorldPlayerId playerId)
        {
            WorldRaceDefinition race = Context.Engine.Definitions.GetRace(state.Players[playerId].Race);
            Dictionary<LeaderRole, int> stacksByRole = CountStacksByRole(state, playerId);

            foreach (UnitDefinitionId leaderId in race.Leaders.OrderBy(GetLeaderOrder))
            {
                UnitDefinition leader = GetLeaderDefinition(leaderId);
                LeaderRole? role = GetLeaderRole(leader.LeaderCategory);

                if (role is not null)
                {
                    int count = stacksByRole.GetValueOrDefault(role.Value);

                    if (count < GetLeaderQuota(state, playerId, role.Value))
                    {
                        yield return (leader, count);
                    }
                }
            }
        }

        private UnitDefinition GetLeaderDefinition(UnitDefinitionId id)
        {
            return Context.Engine.Definitions.GetUnit(id);
        }

        private int GetLeaderOrder(UnitDefinitionId id)
        {
            UnitDefinition leader = GetLeaderDefinition(id);

            return leader.LeaderCategory switch
            {
                UnitLeaderCategory.Fighter => 0,
                UnitLeaderCategory.Rod => 1,
                UnitLeaderCategory.Explorer => 2,
                UnitLeaderCategory.Mage => 3,
                UnitLeaderCategory.Thief => 4,
                null => 5
            };
        }

        private static LeaderRole? GetLeaderRole(UnitLeaderCategory? category)
        {
            return category switch
            {
                UnitLeaderCategory.Fighter => LeaderRole.Fighter,
                UnitLeaderCategory.Explorer => LeaderRole.Explorer,
                UnitLeaderCategory.Rod => LeaderRole.Rod,
                UnitLeaderCategory.Thief => LeaderRole.Thief,
                _ => null
            };
        }

        private static int GetLeaderQuota(WorldState state, WorldPlayerId playerId, LeaderRole role)
        {
            return role switch
            {
                LeaderRole.Fighter => CountFortifications(state, playerId),
                LeaderRole.Explorer => 1,
                LeaderRole.Rod => 1,
                LeaderRole.Thief => 1
            };
        }

        private static int CountFortifications(WorldState state, WorldPlayerId playerId)
        {
            int count = 0;

            foreach (WorldFortification fortification in state.Fortifications.Values)
            {
                if (fortification.Owner == playerId)
                {
                    count++;
                }
            }

            return count;
        }

        private Dictionary<LeaderRole, int> CountStacksByRole(WorldState state, WorldPlayerId playerId)
        {
            Dictionary<LeaderRole, int> counts = [];

            foreach (WorldStack stack in state.Stacks.Values)
            {
                if (stack.OwnerId == playerId && stack is { Order: WorldStackOrder.Normal, AiIgnore: false })
                {
                    (WorldUnit _, GridPosition _, UnitDefinition leaderDefinition) = stack.GetLeader(Context.Engine.Definitions);
                    LeaderRole? role = GetLeaderRole(leaderDefinition.LeaderCategory);

                    if (role is not null)
                    {
                        counts[role.Value] = counts.GetValueOrDefault(role.Value) + 1;
                    }
                }
            }

            return counts;
        }
    }
}
