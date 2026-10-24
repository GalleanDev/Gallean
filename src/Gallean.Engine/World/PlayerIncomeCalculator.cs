namespace Gallean.Engine;

internal static class PlayerIncomeCalculator
{
    public static WorldResourceSet GetIncome(WorldPlayer player, WorldState state, IEngineContext context)
    {
        if (!WorldHelpers.IsRacePlayable(player.Race))
        {
            return WorldResourceSet.Empty;
        }

        WorldResourceSet income = context.Definitions.GetRace(player.Race).Income;
        WorldPlan plan = WorldPlan.For(state);

        foreach (WorldResourceNode node in state.ResourceNodes.Values)
        {
            if (plan.GetLandOwner(node.Position) == player.Id)
            {
                income = ResourceSetCalculator.Add(income, ResourceNodeCalculator.GetIncome(node, context.Globals));
            }
        }

        income = ResourceSetCalculator.ScalePercent(income, 100 + GetDifficultyBonusPercent(state.Difficulty, context.Globals));

        if (player.IsAi)
        {
            WorldAiAttitudeDefinition attitude = context.Definitions.GetAiAttitude(player.AiAttitude);
            income = ResourceSetCalculator.ScalePercent(income, GetAttitudePercent(attitude, state.Difficulty));
        }

        return income;
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
