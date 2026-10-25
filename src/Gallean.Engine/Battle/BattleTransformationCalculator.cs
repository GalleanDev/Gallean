using System.Collections.Immutable;

namespace Gallean.Engine;

internal static class BattleTransformationCalculator
{
    public static BattleState Transform(
        BattleState state,
        IEngineContext context,
        BattleUnitId targetId,
        BattleTransformType type,
        UnitDefinitionId definitionId,
        int level,
        int hitpointsAfter,
        int? remainingTurns)
    {
        state = Restore(state, context, targetId);
        BattleUnit target = state.GetUnit(targetId);
        BattleTransformStatusEffect effect = new(type, target.DefinitionId, target.Level, target.Wards, remainingTurns);

        return Apply(state, context, targetId, definitionId, level, hitpointsAfter, effect);
    }

    public static BattleTransformStatusEffect? GetTransformEffect(BattleUnit unit)
    {
        return unit.StatusEffects.OfType<BattleTransformStatusEffect>().SingleOrDefault();
    }

    public static BattleUnitTransformRestoredEvent GetRestoreEvent(BattleUnitId unitId, BattleTransformStatusEffect effect)
    {
        return new BattleUnitTransformRestoredEvent(unitId, effect.Type, effect.RestoreDefinitionId);
    }

    public static BattleState Restore(BattleState state, IEngineContext context, BattleUnitId unitId)
    {
        BattleUnit unit = state.GetUnit(unitId);
        BattleTransformStatusEffect? effect = GetTransformEffect(unit);

        if (effect is null)
        {
            return state;
        }

        BattleUnit restoredUnit = unit with
        {
            DefinitionId = effect.RestoreDefinitionId,
            Level = effect.RestoreLevel,
            StatusEffects = unit.StatusEffects.Remove(effect),
            Wards = effect.RestoreWards
        };

        restoredUnit = restoredUnit with { Hitpoints = GetScaledHitpoints(state, context, restoredUnit) };

        return state.WithUnit(restoredUnit);
    }

    public static ImmutableArray<BattleEvent> GetRestoreEvents(BattleState state)
    {
        ImmutableArray<BattleEvent>.Builder events = ImmutableArray.CreateBuilder<BattleEvent>();

        foreach (BattleUnitId unitId in state.GetAllUnitIds())
        {
            if (GetTransformEffect(state.GetUnit(unitId)) is { } effect)
            {
                events.Add(GetRestoreEvent(unitId, effect));
            }
        }

        return events.ToImmutable();
    }

    public static int GetTransformedHitpoints(
        BattleState state,
        IEngineContext context,
        BattleUnitId targetId,
        BattleTransformType type,
        UnitDefinitionId definitionId,
        int level)
    {
        BattleState restoredState = Restore(state, context, targetId);
        BattleUnit target = restoredState.GetUnit(targetId);
        BattleTransformStatusEffect effect = new(type, target.DefinitionId, target.Level, target.Wards, null);

        BattleUnit transformed = target with
        {
            DefinitionId = definitionId,
            Level = level,
            StatusEffects = target.StatusEffects.Add(effect)
        };

        return GetScaledHitpoints(restoredState, context, transformed);
    }

    private static int GetScaledHitpoints(BattleState state, IEngineContext context, BattleUnit changedUnit)
    {
        BattleUnit unit = state.GetUnit(changedUnit.Id);
        UnitStats stats = context.UnitStats.GetBattleUnitStats(state, unit.Id);
        UnitStats changedStats = context.UnitStats.GetBattleUnitStats(state.WithUnit(changedUnit), unit.Id);

        if (unit.Hitpoints > 0 && stats.MaxHitpoints != changedStats.MaxHitpoints)
        {
            return Math.Max(1, changedStats.MaxHitpoints * unit.Hitpoints / stats.MaxHitpoints);
        }
        else
        {
            return unit.Hitpoints;
        }
    }

    public static bool IsTransformableCategory(UnitCategory category)
    {
        return category switch
        {
            UnitCategory.Soldier => true,
            UnitCategory.Thief => true,
            UnitCategory.Leader => true,
            UnitCategory.Summon => true,
            UnitCategory.Illusion => false,
            UnitCategory.NeutralLeader => true,
            UnitCategory.NeutralSoldier => true,
            UnitCategory.Guardian => false
        };
    }

    private static BattleState Apply(
        BattleState state,
        IEngineContext context,
        BattleUnitId targetId,
        UnitDefinitionId definitionId,
        int level,
        int hitpointsAfter,
        BattleTransformStatusEffect effect)
    {
        BattleUnit target = state.GetUnit(targetId);

        BattleUnit transformedWithoutWards = target with
        {
            DefinitionId = definitionId,
            Level = level,
            Hitpoints = hitpointsAfter,
            StatusEffects = target.StatusEffects.Add(effect),
            Wards = []
        };

        BattleUnit transformedUnit = transformedWithoutWards with
        {
            Wards = BattleUnitWardCalculator.GetInitialWards(transformedWithoutWards, context.Definitions)
        };

        return state.WithUnit(transformedUnit);
    }
}
