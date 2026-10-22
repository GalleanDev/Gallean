using System.Diagnostics.CodeAnalysis;
using Gallean.Common;

namespace Gallean.Engine;

internal static partial class WorldHealCalculator
{
    public sealed record HealInfo(int Hitpoints, WorldResourceSet Cost, WorldCommandError? Error);

    public sealed record ResurrectInfo(int Hitpoints, WorldResourceSet Cost, WorldCommandError? Error);

    public static HealInfo GetHealInfo(WorldState state, WorldUnitGroup group, WorldUnitId unitId, IEngineContext context)
    {
        if (!TryGetTarget(state, group, unitId, context, out WorldUnit? unit, out WorldCommandError? targetError))
        {
            return new HealInfo(0, WorldResourceSet.Empty, targetError);
        }

        if (!unit.IsAlive)
        {
            return new HealInfo(0, WorldResourceSet.Empty, WorldCommandError.UnitDead);
        }

        UnitCosts costs = context.UnitStats.GetUnitCosts(unit.DefinitionId, unit.Level);

        if (costs.HealCost == WorldResourceSet.Empty)
        {
            return new HealInfo(0, WorldResourceSet.Empty, WorldCommandError.UnitHealForbidden);
        }

        UnitStats stats = context.UnitStats.GetWorldUnitStats(unit);

        if (unit.Hitpoints >= stats.MaxHitpoints)
        {
            return new HealInfo(stats.MaxHitpoints, WorldResourceSet.Empty, WorldCommandError.UnitNotWounded);
        }

        int missingHitpoints = stats.MaxHitpoints - unit.Hitpoints;
        WorldResourceSet cost = ResourceSetCalculator.Multiply(costs.HealCost, missingHitpoints);

        if (!ResourceSetCalculator.CanAfford(state.CurrentPlayer.Resources, cost))
        {
            return new HealInfo(stats.MaxHitpoints, cost, WorldCommandError.InsufficientResources);
        }

        return new HealInfo(stats.MaxHitpoints, cost, Error: null);
    }

    public static ResurrectInfo GetResurrectInfo(WorldState state, WorldUnitGroup group, WorldUnitId unitId, IEngineContext context)
    {
        if (!TryGetTarget(state, group, unitId, context, out WorldUnit? unit, out WorldCommandError? targetError))
        {
            return new ResurrectInfo(0, WorldResourceSet.Empty, targetError);
        }

        if (unit.IsAlive)
        {
            return new ResurrectInfo(0, WorldResourceSet.Empty, WorldCommandError.UnitNotDead);
        }

        UnitCosts costs = context.UnitStats.GetUnitCosts(unit.DefinitionId, unit.Level);
        int hitpoints = context.Globals.ResurrectionHitpoints;

        if (costs.ReviveCost == WorldResourceSet.Empty)
        {
            return new ResurrectInfo(hitpoints, WorldResourceSet.Empty, WorldCommandError.UnitResurrectForbidden);
        }

        if (!ResourceSetCalculator.CanAfford(state.CurrentPlayer.Resources, costs.ReviveCost))
        {
            return new ResurrectInfo(hitpoints, costs.ReviveCost, WorldCommandError.InsufficientResources);
        }

        return new ResurrectInfo(hitpoints, costs.ReviveCost, Error: null);
    }

    public static UnitGrid<WorldUnit> RegenerateStack(WorldState state, WorldStack stack, IEngineContext context)
    {
        WorldPlan plan = WorldPlan.For(state);
        WorldFortification? fortification = plan.GetFortificationAt(stack.Position);

        if (fortification is not null && FortificationCalculator.GetOwner(fortification) == stack.OwnerId)
        {
            return fortification.Accept(RegenerationVisitor.Instance, new RegenerationVisitor.Context(state, stack.Units, context));
        }
        else
        {
            WorldPlayer owner = state.Players[stack.OwnerId];
            WorldRaceDefinition ownerRace = context.Definitions.GetRace(owner.Race);
            WorldMapCell cell = state.Map.GetCellAt(stack.Position);
            int terrainRegeneration = GetTerrainRegeneration(cell, ownerRace, context.Globals);
            int lordRegeneration = GetLordRegeneration(state, owner, context);

            return RegenerateUnits(stack.Units, lordRegeneration + terrainRegeneration, context);
        }
    }

    public static UnitGrid<WorldUnit> RegenerateGarrison(WorldState state, WorldFortification fortification, IEngineContext context)
    {
        return fortification.Accept(RegenerationVisitor.Instance, new RegenerationVisitor.Context(state, fortification.Garrison, context));
    }

    private static UnitGrid<WorldUnit> RegenerateUnits(UnitGrid<WorldUnit> units, int bonusRegeneration, IEngineContext context)
    {
        UnitGrid<WorldUnit>.Builder builder = new();

        foreach ((WorldUnit unit, GridPosition position) in units.GetUnits())
        {
            UnitDefinition definition = context.Definitions.GetUnit(unit.DefinitionId);
            UnitStats stats = context.UnitStats.GetWorldUnitStats(unit);
            WorldUnit updatedUnit = RegenerateUnit(unit, stats, stats.Regeneration + bonusRegeneration);

            builder.Place(updatedUnit, position, definition.IsLarge);
        }

        return builder.Build();
    }

    private static WorldUnit RegenerateUnit(WorldUnit unit, UnitStats stats, int regeneration)
    {
        if (unit.IsAlive)
        {
            int regeneratedHitpoints = stats.MaxHitpoints * regeneration / 100;

            return unit with { Hitpoints = Math.Min(stats.MaxHitpoints, unit.Hitpoints + regeneratedHitpoints) };
        }
        else
        {
            return unit;
        }
    }

    private static int GetLordRegeneration(WorldState state, WorldPlayer player, IEngineContext context)
    {
        WorldPlan plan = WorldPlan.For(state);

        if (plan.IsPlayerDefeated(player.Id) || !WorldHelpers.IsRacePlayable(player.Race))
        {
            return 0;
        }
        else
        {
            LordDefinition lord = context.Definitions.GetLord(player.LordId);

            return lord.Category switch
            {
                LordCategory.Warrior => context.Globals.LordWarriorRegeneration,
                LordCategory.Mage => 0,
                LordCategory.Guildmaster => 0
            };
        }
    }

    private static int GetTerrainRegeneration(WorldMapCell cell, WorldRaceDefinition race, GlobalVariables globals)
    {
        if (cell.Terrain == race.NativeTerrain)
        {
            return globals.NativeTerrainRegeneration;
        }
        else
        {
            return 0;
        }
    }

    private static bool TryGetTarget(
        WorldState state,
        WorldUnitGroup group,
        WorldUnitId unitId,
        IEngineContext context,
        [NotNullWhen(true)] out WorldUnit? unit,
        [NotNullWhen(false)] out WorldCommandError? error)
    {
        unit = null;

        if (!WorldUnitGroupCalculator.TryGetCity(state, group, out _, out error))
        {
            return false;
        }

        if (!PlayerBuildingsCalculator.IsAcquired(state.CurrentPlayer.Buildings, WorldPlayerBuildingCategory.Heal, context.Definitions))
        {
            error = WorldCommandError.HealBuildingRequired;
            return false;
        }

        UnitGrid<WorldUnit> units = WorldUnitGroupCalculator.GetUnits(state, group);

        if (!units.TryGetUnit(unitId, out (WorldUnit Unit, GridPosition Position) entry))
        {
            error = WorldCommandError.UnitNotFound;
            return false;
        }

        unit = entry.Unit;

        return true;
    }
}
