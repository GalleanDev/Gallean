namespace Gallean.Engine;

internal static class PlayerIncomeCalculator
{
    public sealed record IncomeInfo(
        WorldResourceSet Total,
        WorldResourceSet Capital,
        WorldResourceSet ResourceNodes);

    private const int PercentMax = 100;

    public static IncomeInfo GetIncomeInfo(WorldPlayer player, WorldState state, IEngineContext context)
    {
        if (WorldHelpers.IsRacePlayable(player.Race))
        {
            WorldResourceSet raceIncome = context.Definitions.GetRace(player.Race).Income;
            WorldResourceSet nodesIncome = WorldResourceSet.Empty;
            WorldPlan plan = WorldPlan.For(state);

            foreach (WorldResourceNode node in state.ResourceNodes.Values)
            {
                if (plan.GetLandOwner(node.Position) == player.Id)
                {
                    nodesIncome = ResourceSetCalculator.Add(nodesIncome, ResourceNodeCalculator.GetIncome(node, context.Globals));
                }
            }

            WorldResourceSet income = Scale(ResourceSetCalculator.Add(raceIncome, nodesIncome), player, state, context);
            WorldResourceSet paidRaceIncome = Scale(raceIncome, player, state, context);
            WorldResourceSet paidNodesIncome = ResourceSetCalculator.Subtract(income, paidRaceIncome);

            return new IncomeInfo(income, paidRaceIncome, paidNodesIncome);
        }
        else
        {
            return new IncomeInfo(WorldResourceSet.Empty, WorldResourceSet.Empty, WorldResourceSet.Empty);
        }
    }

    public static WorldResourceSet GetResourceNodeIncome(WorldPlayer player, WorldResourceNode node, WorldState state, IEngineContext context)
    {
        WorldResourceSet income = ResourceNodeCalculator.GetIncome(node, context.Globals);

        return Scale(income, player, state, context);
    }

    private static WorldResourceSet Scale(WorldResourceSet income, WorldPlayer player, WorldState state, IEngineContext context)
    {
        WorldResourceSet result = ResourceSetCalculator.ScalePercent(income, PercentMax + GetDifficultyBonusPercent(state.Difficulty, context.Globals));

        if (player.IsAi)
        {
            WorldAiAttitudeDefinition attitude = context.Definitions.GetAiAttitude(player.AiAttitude);

            result = ResourceSetCalculator.ScalePercent(result, GetAttitudePercent(attitude, state.Difficulty));
        }

        return result;
    }

    private static int GetDifficultyBonusPercent(WorldDifficulty difficulty, GlobalVariables globals)
    {
        return difficulty switch
        {
            WorldDifficulty.Easy => globals.IncomeBonusPercentEasy,
            WorldDifficulty.Average => globals.IncomeBonusPercentAverage,
            WorldDifficulty.Hard => globals.IncomeBonusPercentHard,
            WorldDifficulty.VeryHard => globals.IncomeBonusPercentVeryHard
        };
    }

    private static int GetAttitudePercent(WorldAiAttitudeDefinition attitude, WorldDifficulty difficulty)
    {
        return difficulty switch
        {
            WorldDifficulty.Easy => attitude.EasyIncomePercent,
            WorldDifficulty.Average => attitude.AverageIncomePercent,
            WorldDifficulty.Hard => attitude.HardIncomePercent,
            WorldDifficulty.VeryHard => attitude.VeryHardIncomePercent
        };
    }
}
