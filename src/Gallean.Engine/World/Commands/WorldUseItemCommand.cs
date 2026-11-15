using System.Diagnostics.CodeAnalysis;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldUseItemCommand(
    WorldUnitGroup Group,
    WorldItemId ItemId,
    WorldItemTarget? Target) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        return Group switch
        {
            WorldUnitGroup.Stack stackGroup => ExecuteForStack(state, context, stackGroup),
            WorldUnitGroup.Fortification fortificationGroup => ExecuteForFortification(state, context, fortificationGroup)
        };
    }

    private WorldCommandResult ExecuteForStack(WorldState state, IEngineContext context, WorldUnitGroup.Stack stackGroup)
    {
        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, stackGroup.StackId, out WorldStack? stack))
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
        }

        WorldPlan plan = WorldPlan.For(state);
        WorldFortification? fortification = plan.GetFortificationAt(stack.Position);

        if (fortification is not null && FortificationCalculator.IsRioting(fortification))
        {
            return WorldCommandResult.Fail(WorldCommandError.CityRioting);
        }

        return UseItem(state, context);
    }

    private WorldCommandResult ExecuteForFortification(WorldState state, IEngineContext context, WorldUnitGroup.Fortification fortificationGroup)
    {
        WorldFortificationId id = fortificationGroup.FortificationId;

        if (!WorldCommandHelpers.TryGetCurrentPlayerFortification(state, id, out WorldFortification? fortification))
        {
            return WorldCommandResult.Fail(WorldCommandError.GarrisonNotFound);
        }

        if (fortification is not (WorldCity or WorldCapital))
        {
            return WorldCommandResult.Fail(WorldCommandError.GarrisonNotFound);
        }

        if (FortificationCalculator.IsRioting(fortification))
        {
            return WorldCommandResult.Fail(WorldCommandError.CityRioting);
        }

        return UseItem(state, context);
    }

    private WorldCommandResult UseItem(WorldState state, IEngineContext context)
    {
        if (Target is WorldItemTarget.Unit unitTarget && unitTarget.Group != Group)
        {
            if (!InventoryTransferCalculator.TryTransferItem(state, Group, unitTarget.Group, ItemId, out _, out WorldCommandError? transferError))
            {
                return WorldCommandResult.Fail(transferError.Value);
            }
        }

        if (!TryGetItem(state, Group, ItemId, out WorldItem? worldItem))
        {
            return WorldCommandResult.Fail(WorldCommandError.ItemNotFound);
        }

        ItemDefinition item = context.Definitions.GetItem(worldItem.DefinitionId);

        if (item.Effect is null)
        {
            return WorldCommandResult.Fail(WorldCommandError.ItemNotUsable);
        }

        if (LeaderEquipmentCalculator.GetRequiredAbility(item.Category) is { } requiredAbility)
        {
            WorldUnit? leader = FindLeader(state, Group, context.Definitions);

            if (leader is null)
            {
                return WorldCommandResult.Fail(WorldCommandError.MissingLeaderAbility);
            }

            if (!leader.IsAlive)
            {
                return WorldCommandResult.Fail(WorldCommandError.UnitDead);
            }

            if (!context.UnitStats.GetWorldUnitAbilities(leader).HasFlag(requiredAbility))
            {
                return WorldCommandResult.Fail(WorldCommandError.MissingLeaderAbility);
            }
        }

        WorldItemResolver.Context resolution = new(state, context, worldItem, Target);
        WorldCommandResult effectResult = item.Effect.Accept(WorldItemResolver.Instance, resolution);

        if (effectResult.Error is not null)
        {
            return effectResult;
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [
                new WorldItemUsedEvent(
                    Group,
                    worldItem,
                    RemainingCharges: null,
                    LeaderEquipmentCalculator.IsConsumable(item.Category)),
                .. effectResult.Events
            ]);
    }

    private static bool TryGetItem(WorldState state, WorldUnitGroup group, WorldItemId itemId, [NotNullWhen(true)] out WorldItem? item)
    {
        WorldInventory inventory = WorldUnitGroupCalculator.GetInventory(state, group);
        LeaderEquipment equipment = WorldUnitGroupCalculator.GetEquipment(state, group);

        if (inventory.TryGetItem(itemId, out item))
        {
            return true;
        }

        foreach (WorldItem equippedItem in equipment.Items.Values)
        {
            if (equippedItem.Id == itemId)
            {
                item = equippedItem;

                return true;
            }
        }

        item = null;

        return false;
    }

    private static WorldUnit? FindLeader(WorldState state, WorldUnitGroup group, IGameDefinitionProvider definitions)
    {
        UnitGrid<WorldUnit> units = WorldUnitGroupCalculator.GetUnits(state, group);

        foreach ((WorldUnit unit, GridPosition _) in units.GetUnits())
        {
            UnitDefinition definition = definitions.GetUnit(unit.DefinitionId);

            if (definition.Leadership is > 0)
            {
                return unit;
            }
        }

        return null;
    }
}
