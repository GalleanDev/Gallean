using Gallean.Common;

namespace Gallean.Engine;

internal sealed partial class WorldAiEstimator(IEngineContext context)
{
    private readonly IEngineContext engineContext = new EngineContext(
        new FixedRandomProvider(),
        context.Definitions,
        context.UnitStats,
        context.Scenario,
        context.Globals);

    public PowerEstimate EstimatePower(BattleState state)
    {
        int attackerPower = EstimateStackPower(state, BattleSide.Attacker);
        int defenderPower = EstimateStackPower(state, BattleSide.Defender);

        return new PowerEstimate(attackerPower, defenderPower);
    }

    public WorldState? EstimateCommand(WorldState state, IWorldCommand<Empty> command)
    {
        WorldEngine engine = new(engineContext, state);
        WorldCommandResult commandResult = engine.DispatchCommand(command);

        if (commandResult.Failed)
        {
            return null;
        }

        return engine.State;
    }

    public BattleEstimate EstimateBattle(BattleState initialState)
    {
        BattleEngine engine = new(engineContext, initialState, NullBattleUnitUpgradeResolver.Instance);

        engine.DispatchCommand(new BattleStartCommand());
        engine.DispatchCommand(new BattleAutoResolveCommand());

        BattleState finalState = engine.State;
        BattleOutcome? outcome = finalState.Outcome;
        if (outcome is null)
        {
            throw new InvalidOperationException("Auto-resolve completed without a battle outcome.");
        }

        (int unitsBefore, int hitpointsBefore, int experienceBefore, int experienceDeadBefore, int _) = GetStats(initialState);
        (int unitsAfter, int hitpointsAfter, int _, int experienceDeadAfter, int experienceGained) = GetStats(finalState);
        int experienceLost = experienceDeadAfter - experienceDeadBefore;

        return new BattleEstimate(
            outcome,
            unitsBefore,
            unitsAfter,
            hitpointsBefore,
            hitpointsAfter,
            experienceBefore,
            experienceLost,
            experienceGained,
            IsAttackerLeaderAlive(finalState));
    }

    private bool IsAttackerLeaderAlive(BattleState state)
    {
        foreach ((BattleUnit unit, _) in state.AttackerGrid.GetUnits())
        {
            UnitDefinition definition = engineContext.Definitions.GetUnit(unit.DefinitionId);

            if (definition.Leadership is > 0)
            {
                return unit.IsAlive;
            }
        }

        return false;
    }

    private int EstimateStackPower(BattleState state, BattleSide side)
    {
        UnitGrid<BattleUnit> units = state.GetGrid(side);
        UnitGrid<BattleUnit> targets = state.GetGrid(side.GetOpposite());
        int targetCount = targets.GetUnits().Count(entry => entry.Unit.IsAlive);

        int power = 0;

        foreach ((BattleUnit unit, _) in units.GetUnits())
        {
            if (unit.IsAlive)
            {
                UnitDefinition definition = context.Definitions.GetUnit(unit.DefinitionId);
                UnitStats stats = context.UnitStats.GetBattleUnitStats(state, unit.Id);
                power += EstimateUnitPower(unit.Hitpoints, definition, stats, targetCount);
            }
        }

        return power;
    }

    private int EstimateUnitPower(int hitpoints, UnitDefinition definition, UnitStats stats, int targetCount)
    {
        if (hitpoints <= 0)
        {
            return 0;
        }

        int armor = Math.Min(stats.Armor, context.Globals.MaxArmor);
        long effectiveHitpoints = (long)hitpoints * 100 / (100 - armor);
        int impact = EstimateImpact(definition, stats);

        if (definition.AttacksTwice)
        {
            impact *= 2;
        }

        if (definition.Attack.Reach == UnitAttackReach.All)
        {
            impact = impact * (10 + (4 * (targetCount - 1))) / 10;
        }

        return Math.Max(1, (int)Math.Round(Math.Sqrt(effectiveHitpoints * impact)));
    }

    private static int EstimateImpact(UnitDefinition definition, UnitStats stats)
    {
        int impact = definition.Attack.Type switch
        {
            UnitAttackType.Damage => stats.Damage * stats.Accuracy / 100,
            UnitAttackType.DrainLife => stats.Damage * stats.Accuracy / 100,
            UnitAttackType.Poison => stats.Damage * stats.Accuracy / 100,
            UnitAttackType.Frostbite => stats.Damage * stats.Accuracy / 100,
            UnitAttackType.DrainLifeOverflow => stats.Damage * stats.Accuracy / 100,
            UnitAttackType.Blister => stats.Damage * stats.Accuracy / 100,
            UnitAttackType.Heal => definition.Attack.Heal,
            UnitAttackType.Revive => definition.Attack.Heal,
            UnitAttackType.Paralyze => EstimateSpecialImpact(stats),
            UnitAttackType.Fear => EstimateSpecialImpact(stats),
            UnitAttackType.IncreaseDamage => EstimateSpecialImpact(stats),
            UnitAttackType.Petrify => EstimateSpecialImpact(stats),
            UnitAttackType.ReduceDamage => EstimateSpecialImpact(stats),
            UnitAttackType.ReduceInitiative => EstimateSpecialImpact(stats),
            UnitAttackType.Cure => EstimateSpecialImpact(stats),
            UnitAttackType.Summon => EstimateSpecialImpact(stats),
            UnitAttackType.ReduceLevel => EstimateSpecialImpact(stats),
            UnitAttackType.GiveAdditionalAttack => EstimateSpecialImpact(stats),
            UnitAttackType.Doppelganger => EstimateSpecialImpact(stats),
            UnitAttackType.TransformSelf => EstimateSpecialImpact(stats),
            UnitAttackType.TransformEnemy => EstimateSpecialImpact(stats),
            UnitAttackType.GiveProtection => EstimateSpecialImpact(stats),
            UnitAttackType.ReduceArmor => EstimateSpecialImpact(stats)
        };

        return Math.Max(1, impact);

        static int EstimateSpecialImpact(UnitStats stats)
        {
            return stats.ExperienceKilled * stats.ExperienceKilled / Math.Max(1, stats.MaxHitpoints);
        }
    }

    private static (int Units, int Hitpoints, int Experience, int ExperienceLost, int ExperienceGained) GetStats(BattleState state)
    {
        int units = 0;
        int hitpoints = 0;
        int experience = 0;
        int experienceLost = 0;
        int experienceGained = 0;

        foreach ((BattleUnit unit, _) in state.AttackerGrid.GetUnits())
        {
            if (unit.IsAlive)
            {
                units++;
                hitpoints += unit.Hitpoints;
                experience += unit.Experience;
                experienceGained += unit.ExperienceGained;
            }
            else
            {
                experienceLost += unit.Experience + unit.ExperienceGained;
            }
        }

        return (units, hitpoints, experience, experienceLost, experienceGained);
    }
}
