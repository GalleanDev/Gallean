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
        bool isMaximumLevel;
        bool isPendingUpgrade;

        if (CanGainExperience(definition.Category))
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

        return new UnitExperience(experience, stats.ExperienceNextLevel, isMaximumLevel, isPendingUpgrade);
    }

    private static bool CanGainExperience(UnitCategory category)
    {
        return category switch
        {
            UnitCategory.Soldier => true,
            UnitCategory.Thief => true,
            UnitCategory.Leader => true,
            UnitCategory.Summon => false,
            UnitCategory.Illusion => false,
            UnitCategory.NeutralLeader => true,
            UnitCategory.NeutralSoldier => true,
            UnitCategory.Guardian => false
        };
    }
}
