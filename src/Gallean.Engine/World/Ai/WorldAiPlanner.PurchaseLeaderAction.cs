namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed record PurchaseLeaderAction(WorldAiContext Context, int Priority) : AbstractEconomyAction(Context, Priority)
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
            IReadOnlyList<UnitDefinition> wanted = [.. EnumerateWantedLeaders(state, playerId)];

            foreach (UnitDefinition leader in wanted)
            {
                WorldPurchaseLeaderCommand command = new(capital.Id, leader.Id);

                if (!command.Execute(state, Context.Engine).Failed)
                {
                    int score = UtilityScore.InverseRatio(leader.PurchaseCost.Gold, player.Resources.Gold).Score;

                    return MakePlan(score, command, memory);
                }
            }

            foreach (UnitDefinition leader in wanted)
            {
                WorldPurchaseLeaderCommand command = new(capital.Id, leader.Id);
                WorldCommandResult commandResult = command.Execute(state, Context.Engine);

                if (commandResult.Error is WorldCommandError.InsufficientResources &&
                    CanOvercommit(player, leader.PurchaseCost))
                {
                    int score = UtilityScore.InverseRatio(leader.PurchaseCost.Gold, player.Resources.Gold).Score;

                    return MakePlan(score, command, memory);
                }
            }

            return null;
        }

        private IEnumerable<UnitDefinition> EnumerateWantedLeaders(WorldState state, WorldPlayerId playerId)
        {
            WorldRaceDefinition race = Context.Engine.Definitions.GetRace(state.Players[playerId].Race);
            Dictionary<LeaderRole, int> stacksByRole = CountStacksByRole(state, playerId);

            foreach (UnitDefinitionId leaderId in race.Leaders.OrderBy(GetLeaderOrder))
            {
                UnitDefinition leader = GetLeaderDefinition(leaderId);
                LeaderRole? role = GetLeaderRole(leader.LeaderCategory);

                if (role is not null && stacksByRole.GetValueOrDefault(role.Value) < GetLeaderQuota(role.Value))
                {
                    yield return leader;
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

        private static int GetLeaderQuota(LeaderRole role)
        {
            return role switch
            {
                LeaderRole.Fighter => 1,
                LeaderRole.Explorer => 1,
                LeaderRole.Rod => 1,
                LeaderRole.Thief => 1
            };
        }

        private Dictionary<LeaderRole, int> CountStacksByRole(WorldState state, WorldPlayerId playerId)
        {
            Dictionary<LeaderRole, int> counts = [];

            foreach (WorldStack stack in state.Stacks.Values)
            {
                if (stack.OwnerId == playerId)
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
