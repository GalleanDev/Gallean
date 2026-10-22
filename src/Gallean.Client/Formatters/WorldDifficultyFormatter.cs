namespace Gallean.Client;

internal static class WorldDifficultyFormatter
{
    public static string Format(WorldDifficulty difficulty)
    {
        return difficulty switch
        {
            WorldDifficulty.Easy => Tr("Easy"),
            WorldDifficulty.Average => Tr("Average"),
            WorldDifficulty.Hard => Tr("Hard"),
            WorldDifficulty.VeryHard => Tr("Very hard")
        };
    }
}
