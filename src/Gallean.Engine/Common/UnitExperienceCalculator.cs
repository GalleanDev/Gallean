namespace Gallean.Engine;

internal static class UnitExperienceCalculator
{
    public static UnitExperience GetExperience(
        UnitDefinition definition,
        int level,
        int experience,
        UnitStats stats,
        ScenarioDefinition scenario)
    {
        bool canGainExperience = definition.Category is not (UnitCategory.Summon or UnitCategory.Illusion);
        bool isMaximumLevel;
        bool isPendingUpgrade;

        if (canGainExperience)
        {
            isMaximumLevel = definition.Leadership is > 0 && level >= scenario.MaxLeaderLevel;

            if (isMaximumLevel)
            {
                isPendingUpgrade = false;
            }
            else
            {
                isPendingUpgrade = experience >= stats.ExperienceNextLevel - 1;
            }
        }
        else
        {
            isMaximumLevel = true;
            isPendingUpgrade = false;
        }

        return new UnitExperience(experience, stats.ExperienceNextLevel, canGainExperience, isMaximumLevel, isPendingUpgrade);
    }
}
