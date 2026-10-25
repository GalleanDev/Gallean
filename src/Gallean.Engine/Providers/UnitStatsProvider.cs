using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed class UnitStatsProvider(
    IGameDefinitionProvider definitions,
    GlobalVariables globals,
    ScenarioDefinition scenario) : IUnitStatsProvider
{
    private sealed record BattleArmorEffects(int ShatteredArmor, int FortificationArmor, bool IsDefending);

    private sealed class BattleArmorEffectVisitor : IBattleStatusEffectVisitor<BattleArmorEffects, BattleArmorEffects>, IStaticInstance<BattleArmorEffectVisitor>
    {
        public BattleArmorEffects Visit(BattleDefendStatusEffect effect, BattleArmorEffects effects) => effects with { IsDefending = true };
        public BattleArmorEffects Visit(BattleParalyzeStatusEffect effect, BattleArmorEffects effects) => effects;
        public BattleArmorEffects Visit(BattlePetrifyStatusEffect effect, BattleArmorEffects effects) => effects;
        public BattleArmorEffects Visit(BattlePoisonStatusEffect effect, BattleArmorEffects effects) => effects;
        public BattleArmorEffects Visit(BattleFrostbiteStatusEffect effect, BattleArmorEffects effects) => effects;
        public BattleArmorEffects Visit(BattleBlisterStatusEffect effect, BattleArmorEffects effects) => effects;
        public BattleArmorEffects Visit(BattleRetreatStatusEffect effect, BattleArmorEffects effects) => effects;
        public BattleArmorEffects Visit(BattleRetreatedStatusEffect effect, BattleArmorEffects effects) => effects;
        public BattleArmorEffects Visit(BattleFortificationStatusEffect effect, BattleArmorEffects effects) => effects with { FortificationArmor = Math.Max(effects.FortificationArmor, effect.Armor) };
        public BattleArmorEffects Visit(BattleIncreaseDamageStatusEffect effect, BattleArmorEffects effects) => effects;
        public BattleArmorEffects Visit(BattleReduceDamageStatusEffect effect, BattleArmorEffects effects) => effects;
        public BattleArmorEffects Visit(BattleReduceInitiativeStatusEffect effect, BattleArmorEffects effects) => effects;
        public BattleArmorEffects Visit(BattleShatterArmorStatusEffect effect, BattleArmorEffects effects) => effects with { ShatteredArmor = effects.ShatteredArmor + effect.Armor };
        public BattleArmorEffects Visit(BattleCureStatusEffect effect, BattleArmorEffects effects) => effects;
        public BattleArmorEffects Visit(BattleTransformStatusEffect effect, BattleArmorEffects effects) => effects;
    }

    private sealed class BattleInitiativeVisitor : IBattleStatusEffectVisitor<int, int>, IStaticInstance<BattleInitiativeVisitor>
    {
        public int Visit(BattleDefendStatusEffect effect, int initiative) => initiative;
        public int Visit(BattleParalyzeStatusEffect effect, int initiative) => initiative;
        public int Visit(BattlePetrifyStatusEffect effect, int initiative) => initiative;
        public int Visit(BattlePoisonStatusEffect effect, int initiative) => initiative;
        public int Visit(BattleFrostbiteStatusEffect effect, int initiative) => initiative;
        public int Visit(BattleBlisterStatusEffect effect, int initiative) => initiative;
        public int Visit(BattleRetreatStatusEffect effect, int initiative) => initiative;
        public int Visit(BattleRetreatedStatusEffect effect, int initiative) => initiative;
        public int Visit(BattleFortificationStatusEffect effect, int initiative) => initiative;
        public int Visit(BattleIncreaseDamageStatusEffect effect, int initiative) => initiative;
        public int Visit(BattleReduceDamageStatusEffect effect, int initiative) => initiative;
        public int Visit(BattleReduceInitiativeStatusEffect effect, int initiative) => initiative - (initiative * effect.Percent / 100);
        public int Visit(BattleShatterArmorStatusEffect effect, int initiative) => initiative;
        public int Visit(BattleCureStatusEffect effect, int initiative) => initiative;
        public int Visit(BattleTransformStatusEffect effect, int initiative) => initiative;
    }

    private sealed class BattleDamageChangeVisitor : IBattleStatusEffectVisitor<int, int>, IStaticInstance<BattleDamageChangeVisitor>
    {
        public int Visit(BattleDefendStatusEffect effect, int damage) => 0;
        public int Visit(BattleParalyzeStatusEffect effect, int damage) => 0;
        public int Visit(BattlePetrifyStatusEffect effect, int damage) => 0;
        public int Visit(BattlePoisonStatusEffect effect, int damage) => 0;
        public int Visit(BattleFrostbiteStatusEffect effect, int damage) => 0;
        public int Visit(BattleBlisterStatusEffect effect, int damage) => 0;
        public int Visit(BattleRetreatStatusEffect effect, int damage) => 0;
        public int Visit(BattleRetreatedStatusEffect effect, int damage) => 0;
        public int Visit(BattleFortificationStatusEffect effect, int damage) => 0;
        public int Visit(BattleIncreaseDamageStatusEffect effect, int damage) => damage * effect.Percent / 100;
        public int Visit(BattleReduceDamageStatusEffect effect, int damage) => -(damage * effect.Percent / 100);
        public int Visit(BattleReduceInitiativeStatusEffect effect, int damage) => 0;
        public int Visit(BattleShatterArmorStatusEffect effect, int damage) => 0;
        public int Visit(BattleCureStatusEffect effect, int damage) => 0;
        public int Visit(BattleTransformStatusEffect effect, int damage) => 0;
    }

    public UnitStats GetBattleUnitStats(BattleState state, BattleUnitId unitId)
    {
        BattleUnit unit = state.GetUnit(unitId);
        UnitStats stats = UnitStatsCalculator.GetStats(definitions.GetUnit(unit.DefinitionId), unit.Level, unit.Modifiers, definitions, globals);
        BattleArmorEffects armorEffects = new(ShatteredArmor: 0, FortificationArmor: 0, IsDefending: false);

        foreach (BattleAbstractStatusEffect effect in unit.StatusEffects)
        {
            armorEffects = effect.Accept(BattleArmorEffectVisitor.Instance, armorEffects);
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
            battleInitiative = effect.Accept(BattleInitiativeVisitor.Instance, battleInitiative);
        }

        return battleInitiative;
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

    public UnitAttackStats GetBattleAttackStats(BattleState state, BattleUnitId unitId, UnitAttackDefinition attack)
    {
        BattleUnit unit = state.GetUnit(unitId);
        UnitDefinition definition = definitions.GetUnit(unit.DefinitionId);

        UnitAttackStats stats;
        if (attack.Id == definition.Attack.Id || attack.Id == definition.Attack.AlternativeAttackId)
        {
            stats = UnitAttackStatsCalculator.CalculateForUnit(definition, attack, unit.Level, unit.Modifiers, definitions, globals);
        }
        else if (attack.Id == definition.SecondaryAttack?.Id)
        {
            stats = UnitAttackStatsCalculator.CalculateForUnit(definition, attack, unit.Level, [], definitions, globals);
        }
        else
        {
            stats = UnitAttackStatsCalculator.CalculateForItem(attack, globals);
        }

        int damageChange = 0;

        foreach (BattleAbstractStatusEffect effect in unit.StatusEffects)
        {
            damageChange += effect.Accept(BattleDamageChangeVisitor.Instance, stats.Damage);
        }

        BattleSideModifiers sideModifiers = state.Modifiers.GetModifiers(unit.Side);

        return stats with
        {
            Damage = Math.Min(stats.Damage + damageChange, globals.MaxAttackDamage),
            Accuracy = Math.Clamp(stats.Accuracy + sideModifiers.AccuracyBonus, 0, globals.MaxAttackAccuracy)
        };
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

        return UnitExperienceCalculator.GetExperience(definitions.GetUnit(unit.DefinitionId), unit.Level, unit.Experience, stats, scenario);
    }

    public UnitStats GetWorldUnitStats(WorldUnit unit)
    {
        return UnitStatsCalculator.GetStats(definitions.GetUnit(unit.DefinitionId), unit.Level, unit.Modifiers, definitions, globals);
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
