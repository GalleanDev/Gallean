using System.Collections.Immutable;

namespace Gallean.Engine;

internal static class UnitStatsCalculator
{
    public static UnitStats GetStats(
        UnitDefinition definition,
        int level,
        ImmutableArray<UnitModifier> modifiers,
        IGameDefinitionProvider definitions,
        GlobalVariables globals)
    {
        UnitDynamicUpgrade upgrade = UnitDynamicUpgradeCalculator.Calculate(definition, level, definitions);

        UnitStats stats = new(
            definition.MaxHitpoints + upgrade.MaxHitpoints,
            definition.Armor + upgrade.Armor,
            definition.Regeneration + upgrade.Regeneration,
            definition.Attack.Initiative + upgrade.Initiative,
            definition.Attack.Damage + upgrade.Damage,
            definition.Attack.Accuracy + upgrade.Accuracy,
            definition.ExperienceKilled + upgrade.ExperienceKilled,
            definition.ExperienceNextLevel + upgrade.ExperienceNextLevel);

        stats = ApplyModifiers(stats, modifiers, definitions, globals);

        return stats with
        {
            MaxHitpoints = Math.Min(stats.MaxHitpoints, globals.MaxHitpoints),
            Armor = Math.Min(stats.Armor, globals.MaxArmor),
            Initiative = Math.Min(stats.Initiative, globals.MaxAttackInitiative),
            Damage = Math.Min(stats.Damage, globals.MaxAttackDamage),
            Accuracy = Math.Min(stats.Accuracy, globals.MaxAttackAccuracy)
        };
    }

    public static int GetLeadership(
        UnitDefinition definition,
        ImmutableArray<UnitModifier> modifiers,
        IGameDefinitionProvider definitions)
    {
        int leadership = definition.Leadership ?? 0;

        foreach (UnitModifier modifier in modifiers)
        {
            UnitModifierDefinition modifierDefinition = definitions.GetUnitModifier(modifier.DefinitionId);
            leadership = ApplyStat(leadership, modifierDefinition.Leadership, 1, GridPosition.Count);
        }

        return leadership;
    }

    public static int GetMaxMovementPoints(
        UnitDefinition definition,
        int level,
        ImmutableArray<UnitModifier> modifiers,
        IGameDefinitionProvider definitions,
        GlobalVariables globals)
    {
        UnitDynamicUpgrade upgrade = UnitDynamicUpgradeCalculator.Calculate(definition, level, definitions);

        int movement = (definition.MovementPoints ?? 0) + upgrade.MovementPoints;

        foreach (UnitModifier modifier in modifiers)
        {
            UnitModifierDefinition modifierDefinition = definitions.GetUnitModifier(modifier.DefinitionId);
            movement = ApplyStat(movement, modifierDefinition.MaxMovement, globals.MinStackMovement, globals.MaxStackMovement);
        }

        return Math.Min(movement, globals.MaxStackMovement);
    }

    public static LeaderAbility GetLeaderAbilities(
        UnitDefinition definition,
        ImmutableArray<UnitModifier> modifiers,
        IGameDefinitionProvider definitions)
    {
        LeaderAbility abilities = definition.LeaderAbilities;

        foreach (UnitModifier modifier in modifiers)
        {
            UnitModifierDefinition modifierDefinition = definitions.GetUnitModifier(modifier.DefinitionId);
            abilities |= modifierDefinition.LeaderAbilities;
        }

        return abilities;
    }

    public static int GetVisionRadius(
        UnitDefinition definition,
        ImmutableArray<UnitModifier> modifiers,
        IGameDefinitionProvider definitions,
        GlobalVariables globals)
    {
        int visionRadius = definition.VisionRadius ?? 0;

        foreach (UnitModifier modifier in modifiers)
        {
            UnitModifierDefinition modifierDefinition = definitions.GetUnitModifier(modifier.DefinitionId);
            visionRadius = ApplyStat(visionRadius, modifierDefinition.VisionRadius, globals.MinStackVisionRadius, globals.MaxStackVisionRadius);
        }

        return Math.Clamp(visionRadius, globals.MinStackVisionRadius, globals.MaxStackVisionRadius);
    }

    public static int GetNegotiate(
        UnitDefinition definition,
        int level,
        IGameDefinitionProvider definitions)
    {
        UnitDynamicUpgrade upgrade = UnitDynamicUpgradeCalculator.Calculate(definition, level, definitions);

        return (definition.Negotiate ?? 0) + upgrade.Negotiate;
    }

    public static UnitCosts GetCosts(
        UnitDefinition definition,
        int level,
        IGameDefinitionProvider definitions)
    {
        UnitDynamicUpgrade upgrade = UnitDynamicUpgradeCalculator.Calculate(definition, level, definitions);

        return new UnitCosts(
            ResourceSetCalculator.Add(definition.PurchaseCost, upgrade.PurchaseCost),
            ResourceSetCalculator.Add(definition.ReviveCost, upgrade.ReviveCost),
            ResourceSetCalculator.Add(definition.HealCost, upgrade.HealCost),
            ResourceSetCalculator.Add(definition.TrainingCost, upgrade.TrainingCost));
    }

    public static ImmutableArray<UnitWard> GetWards(
        UnitDefinition definition,
        ImmutableArray<UnitModifier> modifiers,
        IGameDefinitionProvider definitions)
    {
        ImmutableArray<UnitWard>.Builder wards = ImmutableArray.CreateBuilder<UnitWard>();

        wards.AddRange(definition.Wards);

        foreach (UnitModifier modifier in modifiers)
        {
            UnitModifierDefinition modifierDefinition = definitions.GetUnitModifier(modifier.DefinitionId);
            wards.AddRange(modifierDefinition.Wards);
        }

        return wards.ToImmutable();
    }

    private static UnitStats ApplyModifiers(
        UnitStats stats,
        ImmutableArray<UnitModifier> modifiers,
        IGameDefinitionProvider definitions,
        GlobalVariables globals)
    {
        foreach (UnitModifier modifier in modifiers)
        {
            UnitModifierDefinition modifierDefinition = definitions.GetUnitModifier(modifier.DefinitionId);

            stats = stats with
            {
                MaxHitpoints = ApplyStat(stats.MaxHitpoints, modifierDefinition.Hitpoints, globals.MinHitpoints, globals.MaxHitpoints),
                Armor = ApplyStat(stats.Armor, modifierDefinition.Armor, globals.MinArmor, globals.MaxArmor),
                Regeneration = Math.Max(stats.Regeneration, modifierDefinition.RegenerationPercent),
                Initiative = ApplyStat(stats.Initiative, modifierDefinition.Initiative, globals.MinAttackInitiative, globals.MaxAttackInitiative),
                Damage = ApplyStat(stats.Damage, modifierDefinition.Damage, globals.MinAttackDamage, globals.MaxModifiedAttackDamage),
                Accuracy = ApplyStat(stats.Accuracy, modifierDefinition.Accuracy, globals.MinAttackAccuracy, globals.MaxAttackAccuracy)
            };
        }

        return stats;
    }

    public static int ApplyStat(int value, UnitStatModifier modifier, int min, int max)
    {
        return Math.Clamp(value + modifier.Flat + (value * modifier.Percent / 100), min, max);
    }
}
