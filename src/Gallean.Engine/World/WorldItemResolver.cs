using Gallean.Common;

namespace Gallean.Engine;

internal sealed class WorldItemResolver : IItemEffectVisitor<WorldItemResolver.Context, WorldCommandResult>, IStaticInstance<WorldItemResolver>
{
    internal sealed record Context(
        WorldState State,
        IEngineContext EngineContext,
        WorldStack Stack,
        WorldItem Item,
        WorldItemTarget? Target);

    public WorldCommandResult Visit(HealItemEffect effect, Context context)
    {
        if (context.Target is not WorldItemTarget.Unit unitTarget)
        {
            return WorldCommandResult.Fail(WorldCommandError.ItemTargetInvalid);
        }

        if (!context.Stack.Units.TryGetUnit(unitTarget.UnitId, out (WorldUnit Unit, GridPosition Position) entry))
        {
            return WorldCommandResult.Fail(WorldCommandError.UnitNotFound);
        }

        WorldUnit target = entry.Unit;

        if (!target.IsAlive)
        {
            return WorldCommandResult.Fail(WorldCommandError.UnitDead);
        }

        UnitStats stats = context.EngineContext.UnitStats.GetWorldUnitStats(target);

        if (target.Hitpoints >= stats.MaxHitpoints)
        {
            return WorldCommandResult.Fail(WorldCommandError.UnitNotWounded);
        }

        int hitpointsAfter = Math.Min(target.Hitpoints + effect.Hitpoints, stats.MaxHitpoints);

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldUnitHealedEvent(new WorldUnitGroup.Stack(context.Stack.Id), unitTarget.UnitId, hitpointsAfter, WorldResourceSet.Empty)]);
    }

    public WorldCommandResult Visit(AttackItemEffect effect, Context context)
    {
        return WorldCommandResult.Fail(WorldCommandError.ItemNotUsable);
    }

    public WorldCommandResult Visit(ReviveItemEffect effect, Context context)
    {
        if (context.Target is not WorldItemTarget.Unit unitTarget)
        {
            return WorldCommandResult.Fail(WorldCommandError.ItemTargetInvalid);
        }

        if (!context.Stack.Units.TryGetUnit(unitTarget.UnitId, out (WorldUnit Unit, GridPosition Position) entry))
        {
            return WorldCommandResult.Fail(WorldCommandError.UnitNotFound);
        }

        WorldUnit target = entry.Unit;

        if (target.IsAlive)
        {
            return WorldCommandResult.Fail(WorldCommandError.UnitNotDead);
        }

        UnitCosts costs = context.EngineContext.UnitStats.GetUnitCosts(target.DefinitionId, target.Level);

        if (costs.ReviveCost == WorldResourceSet.Empty)
        {
            return WorldCommandResult.Fail(WorldCommandError.UnitResurrectForbidden);
        }

        int hitpoints = context.EngineContext.Globals.ResurrectionHitpoints;

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldUnitResurrectedEvent(new WorldUnitGroup.Stack(context.Stack.Id), unitTarget.UnitId, hitpoints, WorldResourceSet.Empty)]);
    }

    public WorldCommandResult Visit(TemporaryBoostItemEffect effect, Context context)
    {
        return ApplyModifier(context, effect.ModifierId, context.EngineContext.Globals.PotionBoostDurationTurns);
    }

    public WorldCommandResult Visit(PermanentBoostItemEffect effect, Context context)
    {
        return ApplyModifier(context, effect.ModifierId, remainingTurns: null);
    }

    public WorldCommandResult Visit(SpellItemEffect effect, Context context)
    {
        WorldSpellTarget? target = null;

        if (context.Target is WorldItemTarget.Map map)
        {
            target = map.Target;
        }
        else if (context.Target is not null)
        {
            return WorldCommandResult.Fail(WorldCommandError.ItemTargetInvalid);
        }

        WorldSpellDefinition spell = context.EngineContext.Definitions.GetSpell(effect.SpellId);
        WorldPlayerId casterId = context.Stack.OwnerId;
        WorldPlayer player = context.State.Players[casterId];

        SpellCalculator.CastInfo castInfo = SpellCalculator.GetItemCastInfo(context.Item, player, context.EngineContext);
        if (castInfo.Error is not null)
        {
            return WorldCommandResult.Fail(castInfo.Error.Value);
        }

        if (!SpellTargetCalculator.IsTargetValid(context.State, player, spell, target))
        {
            return WorldCommandResult.Fail(WorldCommandError.SpellTargetInvalid);
        }

        if (SpellTargetCalculator.IsTargetConcealed(context.State, casterId, target))
        {
            return WorldCommandResult.Fail(WorldCommandError.SpellTargetConcealed);
        }

        WorldCommandResult effectResult = WorldSpellEffectResolver.Resolve(context.State, context.EngineContext, casterId, target, spell);
        if (effectResult.Failed)
        {
            return WorldCommandResult.Fail(effectResult.Error.Value);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldSpellCastEvent(casterId, effect.SpellId, target, effect.CastingCost, WorldSpellCastSource.Item)]);
    }

    private static WorldCommandResult ApplyModifier(Context context, UnitModifierDefinitionId modifierId, int? remainingTurns)
    {
        if (context.Target is not WorldItemTarget.Unit unitTarget)
        {
            return WorldCommandResult.Fail(WorldCommandError.ItemTargetInvalid);
        }

        if (!context.Stack.Units.TryGetUnit(unitTarget.UnitId, out (WorldUnit Unit, GridPosition Position) entry))
        {
            return WorldCommandResult.Fail(WorldCommandError.UnitNotFound);
        }

        WorldUnit target = entry.Unit;

        if (!target.IsAlive)
        {
            return WorldCommandResult.Fail(WorldCommandError.UnitDead);
        }

        if (target.Modifiers.Any(modifier => modifier.DefinitionId == modifierId))
        {
            return WorldCommandResult.Fail(WorldCommandError.UnitModifierAlreadyApplied);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [
                new WorldUnitModifierAppliedEvent(
                    new WorldUnitGroup.Stack(context.Stack.Id),
                    unitTarget.UnitId,
                    new UnitModifier(modifierId, remainingTurns))
            ]);
    }
}
