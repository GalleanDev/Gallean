using System.Collections.Immutable;

namespace Gallean.Engine;

internal static partial class ObjectiveCalculator
{
    private readonly record struct Context(WorldState State, IGameScenarioProvider Scenario, WorldPlayerId PlayerId);

    public static ImmutableArray<WorldObjective> GetObjectives(
        IGameScenarioProvider scenario,
        WorldState state,
        WorldPlayerId playerId)
    {
        if (!state.Players.TryGetValue(playerId, out WorldPlayer? player))
        {
            return [];
        }

        Context context = new(state, scenario, playerId);
        TargetVisitor targetVisitor = new();
        HashSet<WorldObjective> result = [];

        foreach (ScenarioEventDefinition definition in scenario.Events)
        {
            if (IsObjective(definition, state, player))
            {
                foreach (ScenarioEventCondition condition in definition.Conditions)
                {
                    foreach (WorldObjective objective in condition.Accept(targetVisitor, context))
                    {
                        result.Add(objective);
                    }
                }
            }
        }

        return
        [
            .. result
                .OrderBy(objective => objective.Footprint.TopLeft.Y)
                .ThenBy(objective => objective.Footprint.TopLeft.X)
                .ThenBy(objective => objective.Footprint.Width)
                .ThenBy(objective => objective.Footprint.Height)
                .ThenBy(objective => objective.StackId)
        ];
    }

    private static bool IsObjective(ScenarioEventDefinition definition, WorldState state, WorldPlayer player)
    {
        if (!ScenarioEventCalculator.IsEnabled(state.ScenarioState, definition))
        {
            return false;
        }

        if (!definition.AffectedRaces.Contains(player.Race))
        {
            return false;
        }

        if (definition.Effects is [.., ScenarioEventWinEffect { WinnerId: { } winnerId }])
        {
            if (state.Players.TryGetValue(winnerId, out WorldPlayer? winner))
            {
                return winner.Race == player.Race;
            }
        }

        return false;
    }
}
