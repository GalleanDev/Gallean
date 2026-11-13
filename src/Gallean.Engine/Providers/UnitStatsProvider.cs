using System.Collections.Immutable;

namespace Gallean.Engine;

public sealed class UnitStatsProvider(
    IGameDefinitionProvider definitions,
    GlobalVariables globals,
    ScenarioDefinition scenario) : IUnitStatsProvider
{
    private sealed record BattleArmorEffects(int ShatteredArmor, int FortificationArmor, bool IsDefending);

    public UnitStats GetBattleUnitStats(BattleState state, BattleUnitId unitId)
    {
        BattleUnit unit = state.GetUnit(unitId);
        UnitStats stats = UnitStatsCalculator.GetStats(definitions.GetUnit(unit.DefinitionId), unit.Level, unit.Modifiers, definitions, globals);
        BattleArmorEffects armorEffects = new(ShatteredArmor: 0, FortificationArmor: 0, IsDefending: false);

        foreach (BattleAbstractStatusEffect effect in unit.StatusEffects)
        {
            armorEffects = ApplyArmorEffect(effect, armorEffects);
        }

        return stats with
        {
            MaxHitpoints = GetBattleMaxHitpoints(unit, stats.MaxHitpoints),
            Armor = GetBattleArmor(stats.Armor, armorEffects),
            Initiative = GetBattleInitiative(unit, stats.Initiative)
        };
    }

    private int GetBattleMaxHitpoints(BattleUnit unit, int maxHitpoints)
    {
        BattleTransformStatusEffect? transform = BattleTransformationCalculator.GetTransformEffect(unit);

        if (transform is not null)
        {
            return transform.Type switch
            {
                BattleTransformType.Transform => GetRestoredMaxHitpoints(unit, transform),
                BattleTransformType.TransformSelf => maxHitpoints,
                BattleTransformType.Doppelganger => maxHitpoints,
                BattleTransformType.LevelDrain => maxHitpoints
            };
        }
        else
        {
            return maxHitpoints;
        }
    }

    private int GetRestoredMaxHitpoints(BattleUnit unit, BattleTransformStatusEffect transform)
    {
        UnitDefinition definition = definitions.GetUnit(transform.RestoreDefinitionId);
        UnitStats stats = UnitStatsCalculator.GetStats(definition, transform.RestoreLevel, unit.Modifiers, definitions, globals);

        return stats.MaxHitpoints;
    }

    public int GetBattleUnitInitiative(BattleState state, BattleUnitId unitId, int roll)
    {
        BattleUnit unit = state.GetUnit(unitId);
        UnitStats stats = UnitStatsCalculator.GetStats(definitions.GetUnit(unit.DefinitionId), unit.Level, unit.Modifiers, definitions, globals);

        return GetBattleInitiative(unit, stats.Initiative + roll);
    }

    private static int GetBattleInitiative(BattleUnit unit, int initiative)
    {
        int battleInitiative = initiative;

        foreach (BattleAbstractStatusEffect effect in unit.StatusEffects)
        {
            battleInitiative = ApplyInitiativeEffect(effect, battleInitiative);
        }

        return battleInitiative;
    }

    private static int ApplyInitiativeEffect(BattleAbstractStatusEffect effect, int initiative)
    {
        return effect switch
        {
            BattleDefendStatusEffect => initiative,
            BattleParalyzeStatusEffect => initiative,
            BattlePetrifyStatusEffect => initiative,
            BattlePoisonStatusEffect => initiative,
            BattleFrostbiteStatusEffect => initiative,
            BattleBlisterStatusEffect => initiative,
            BattleRetreatStatusEffect => initiative,
            BattleRetreatedStatusEffect => initiative,
            BattleFortificationStatusEffect => initiative,
            BattleIncreaseDamageStatusEffect => initiative,
            BattleReduceDamageStatusEffect => initiative,
            BattleReduceInitiativeStatusEffect reduceInitiative => initiative - (initiative * reduceInitiative.Percent / 100),
            BattleShatterArmorStatusEffect => initiative,
            BattleCureStatusEffect => initiative,
            BattleTransformStatusEffect => initiative
        };
    }

    private int GetBattleArmor(int armor, BattleArmorEffects effects)
    {
        int battleArmor = Math.Max(armor - effects.ShatteredArmor, effects.FortificationArmor);

        if (effects.IsDefending)
        {
            battleArmor += globals.DefendArmorBonus * (100 - battleArmor) / 100;
            battleArmor = Math.Min(battleArmor, globals.MaxArmor);
        }

        return battleArmor;
    }

    private static BattleArmorEffects ApplyArmorEffect(BattleAbstractStatusEffect effect, BattleArmorEffects effects)
    {
        return effect switch
        {
            BattleDefendStatusEffect => effects with { IsDefending = true },
            BattleParalyzeStatusEffect => effects,
            BattlePetrifyStatusEffect => effects,
            BattlePoisonStatusEffect => effects,
            BattleFrostbiteStatusEffect => effects,
            BattleBlisterStatusEffect => effects,
            BattleRetreatStatusEffect => effects,
            BattleRetreatedStatusEffect => effects,
            BattleFortificationStatusEffect fortification => effects with { FortificationArmor = Math.Max(effects.FortificationArmor, fortification.Armor) },
            BattleIncreaseDamageStatusEffect => effects,
            BattleReduceDamageStatusEffect => effects,
            BattleReduceInitiativeStatusEffect => effects,
            BattleShatterArmorStatusEffect shatterArmor => effects with { ShatteredArmor = effects.ShatteredArmor + shatterArmor.Armor },
            BattleCureStatusEffect => effects,
            BattleTransformStatusEffect => effects
        };
    }

    public UnitAttackStats GetBattleAttackStats(BattleState state, BattleUnitId unitId, UnitAttackDefinition attack)
    {
        BattleUnit unit = state.GetUnit(unitId);
        UnitDefinition definition = definitions.GetUnit(unit.DefinitionId);

        UnitAttackStats stats = GetAttackStats(definition, unit.Level, unit.Modifiers, attack);

        int damageChange = 0;

        if (attack.Type is UnitAttackType.Damage or UnitAttackType.DrainLife or UnitAttackType.DrainLifeOverflow)
        {
            foreach (BattleAbstractStatusEffect effect in unit.StatusEffects)
            {
                damageChange += GetDamageChange(effect, stats.Damage);
            }
        }

        return stats with { Damage = Math.Min(stats.Damage + damageChange, globals.MaxAttackDamage) };
    }

    private static int GetDamageChange(BattleAbstractStatusEffect effect, int damage)
    {
        return effect switch
        {
            BattleDefendStatusEffect => 0,
            BattleParalyzeStatusEffect => 0,
            BattlePetrifyStatusEffect => 0,
            BattlePoisonStatusEffect => 0,
            BattleFrostbiteStatusEffect => 0,
            BattleBlisterStatusEffect => 0,
            BattleRetreatStatusEffect => 0,
            BattleRetreatedStatusEffect => 0,
            BattleFortificationStatusEffect => 0,
            BattleIncreaseDamageStatusEffect increaseDamage => damage * increaseDamage.Percent / 100,
            BattleReduceDamageStatusEffect reduceDamage => -(damage * reduceDamage.Percent / 100),
            BattleReduceInitiativeStatusEffect => 0,
            BattleShatterArmorStatusEffect => 0,
            BattleCureStatusEffect => 0,
            BattleTransformStatusEffect => 0
        };
    }

    private UnitAttackStats GetAttackStats(UnitDefinition definition, int level, ImmutableArray<UnitModifier> modifiers, UnitAttackDefinition attack)
    {
        if (attack.Id == definition.Attack.Id || attack.Id == definition.Attack.AlternativeAttackId)
        {
            return UnitAttackStatsCalculator.CalculateForUnit(definition, attack, level, modifiers, definitions, globals);
        }
        else if (attack.Id == definition.SecondaryAttack?.Id)
        {
            return UnitAttackStatsCalculator.CalculateForUnit(definition, attack, level, [], definitions, globals);
        }
        else
        {
            return UnitAttackStatsCalculator.CalculateForItem(attack, globals);
        }
    }

    public LeaderAbility GetBattleUnitAbilities(BattleState state, BattleUnitId unitId)
    {
        BattleUnit unit = state.GetUnit(unitId);

        return UnitStatsCalculator.GetLeaderAbilities(definitions.GetUnit(unit.DefinitionId), unit.Modifiers, definitions);
    }

    public int GetBattleUnitLeadership(BattleState state, BattleUnitId unitId)
    {
        BattleUnit unit = state.GetUnit(unitId);

        return UnitStatsCalculator.GetLeadership(definitions.GetUnit(unit.DefinitionId), unit.Modifiers, definitions);
    }

    public UnitExperience GetBattleUnitExperience(BattleState state, BattleUnitId unitId)
    {
        BattleUnit unit = state.GetUnit(unitId);
        UnitStats stats = GetBattleUnitStats(state, unitId);
        int experience;

        if (unit.HasStatusEffect<BattleTransformStatusEffect>())
        {
            experience = 0;
        }
        else
        {
            experience = unit.Experience;
        }

        return UnitExperienceCalculator.GetExperience(definitions.GetUnit(unit.DefinitionId), unit.Level, experience, stats, scenario);
    }

    public UnitStats GetWorldUnitStats(WorldUnit unit)
    {
        return UnitStatsCalculator.GetStats(definitions.GetUnit(unit.DefinitionId), unit.Level, unit.Modifiers, definitions, globals);
    }

    public UnitAttackStats GetWorldAttackStats(WorldUnit unit, UnitAttackDefinition attack)
    {
        return GetAttackStats(definitions.GetUnit(unit.DefinitionId), unit.Level, unit.Modifiers, attack);
    }

    public LeaderAbility GetWorldUnitAbilities(WorldUnit unit)
    {
        return UnitStatsCalculator.GetLeaderAbilities(definitions.GetUnit(unit.DefinitionId), unit.Modifiers, definitions);
    }

    public int GetWorldUnitLeadership(WorldUnit unit)
    {
        return UnitStatsCalculator.GetLeadership(definitions.GetUnit(unit.DefinitionId), unit.Modifiers, definitions);
    }

    public ImmutableArray<UnitWard> GetWorldUnitWards(WorldUnit unit)
    {
        return UnitStatsCalculator.GetWards(definitions.GetUnit(unit.DefinitionId), unit.Modifiers, definitions);
    }

    public UnitExperience GetWorldUnitExperience(WorldUnit unit)
    {
        UnitStats stats = GetWorldUnitStats(unit);

        return UnitExperienceCalculator.GetExperience(definitions.GetUnit(unit.DefinitionId), unit.Level, unit.Experience, stats, scenario);
    }

    public StackStats GetWorldStackStats(WorldStack stack)
    {
        foreach ((WorldUnit unit, _) in stack.Units.GetUnits())
        {
            UnitDefinition definition = definitions.GetUnit(unit.DefinitionId);

            if (definition.Leadership is > 0)
            {
                return new StackStats(
                    UnitStatsCalculator.GetMaxMovementPoints(definition, unit.Level, unit.Modifiers, definitions, globals),
                    UnitStatsCalculator.GetLeadership(definition, unit.Modifiers, definitions),
                    UnitStatsCalculator.GetVisionRadius(definition, unit.Modifiers, definitions, globals),
                    UnitStatsCalculator.GetNegotiate(definition, unit.Level, definitions));
            }
        }

        return new StackStats(0, 0, 0, 0);
    }

    public UnitUpgrade GetUnitUpgrade(UnitDefinitionId unitDefinitionId, bool isTypeLocked, WorldPlayerBuildings buildings)
    {
        return UnitUpgradeCalculator.GetUpgrade(unitDefinitionId, isTypeLocked, buildings, definitions);
    }

    public UnitCosts GetUnitCosts(UnitDefinitionId unitDefinitionId, int level)
    {
        return UnitStatsCalculator.GetCosts(definitions.GetUnit(unitDefinitionId), level, definitions);
    }
}
