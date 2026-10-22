namespace Gallean.Engine;

internal static class UnitDynamicUpgradeCalculator
{
    public static UnitDynamicUpgrade Calculate(UnitDefinition definition, int level, IGameDefinitionProvider definitions)
    {
        if (level > definition.Level)
        {
            UnitDynamicUpgradeDefinition dynamicUpgrade = definitions.GetUnitDynamicUpgrade(definition.Id);

            int gainedLevels = level - definition.Level;
            int lowLevelCount = Math.Min(gainedLevels, Math.Max(0, dynamicUpgrade.UpgradeChangeLevel - definition.Level));
            int highLevelCount = gainedLevels - lowLevelCount;

            return Combine(dynamicUpgrade.LowLevelUpgrade, lowLevelCount, dynamicUpgrade.HighLevelUpgrade, highLevelCount);
        }
        else
        {
            return UnitDynamicUpgrade.Empty;
        }
    }

    private static UnitDynamicUpgrade Combine(
        UnitDynamicUpgrade lowLevelUpgrade,
        int lowLevelCount,
        UnitDynamicUpgrade highLevelUpgrade,
        int highLevelCount)
    {
        return new UnitDynamicUpgrade(
            MaxHitpoints: (lowLevelUpgrade.MaxHitpoints * lowLevelCount) + (highLevelUpgrade.MaxHitpoints * highLevelCount),
            Armor: (lowLevelUpgrade.Armor * lowLevelCount) + (highLevelUpgrade.Armor * highLevelCount),
            Regeneration: (lowLevelUpgrade.Regeneration * lowLevelCount) + (highLevelUpgrade.Regeneration * highLevelCount),
            Initiative: (lowLevelUpgrade.Initiative * lowLevelCount) + (highLevelUpgrade.Initiative * highLevelCount),
            Damage: (lowLevelUpgrade.Damage * lowLevelCount) + (highLevelUpgrade.Damage * highLevelCount),
            Accuracy: (lowLevelUpgrade.Accuracy * lowLevelCount) + (highLevelUpgrade.Accuracy * highLevelCount),
            ExperienceKilled: (lowLevelUpgrade.ExperienceKilled * lowLevelCount) + (highLevelUpgrade.ExperienceKilled * highLevelCount),
            ExperienceNextLevel: (lowLevelUpgrade.ExperienceNextLevel * lowLevelCount) + (highLevelUpgrade.ExperienceNextLevel * highLevelCount),
            MovementPoints: (lowLevelUpgrade.MovementPoints * lowLevelCount) + (highLevelUpgrade.MovementPoints * highLevelCount),
            Negotiate: (lowLevelUpgrade.Negotiate * lowLevelCount) + (highLevelUpgrade.Negotiate * highLevelCount),
            PurchaseCost: CombineCost(lowLevelUpgrade.PurchaseCost, lowLevelCount, highLevelUpgrade.PurchaseCost, highLevelCount),
            ReviveCost: CombineCost(lowLevelUpgrade.ReviveCost, lowLevelCount, highLevelUpgrade.ReviveCost, highLevelCount),
            HealCost: CombineCost(lowLevelUpgrade.HealCost, lowLevelCount, highLevelUpgrade.HealCost, highLevelCount),
            TrainingCost: CombineCost(lowLevelUpgrade.TrainingCost, lowLevelCount, highLevelUpgrade.TrainingCost, highLevelCount));
    }

    private static WorldResourceSet CombineCost(
        WorldResourceSet lowLevelCost,
        int lowLevelCount,
        WorldResourceSet highLevelCost,
        int highLevelCount)
    {
        return ResourceSetCalculator.Add(
            ResourceSetCalculator.Multiply(lowLevelCost, lowLevelCount),
            ResourceSetCalculator.Multiply(highLevelCost, highLevelCount));
    }
}
