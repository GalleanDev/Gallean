using System.Collections.Immutable;

namespace Gallean.Engine;

public sealed class ScenarioExporter(IGameDefinitionProvider definitions, GlobalVariables globals)
{
    public int ItemLimit => globals.LeaderExportItemLimit;

    public ImmutableArray<WorldStack> GetExportableStacks(WorldState state, WorldPlayerId playerId)
    {
        WorldPlayer player = state.Players[playerId];
        WorldRaceDefinition race = definitions.GetRace(player.Race);
        ImmutableArray<WorldStack>.Builder result = ImmutableArray.CreateBuilder<WorldStack>();

        foreach (WorldStack stack in state.Stacks.Values)
        {
            if (stack.OwnerId == playerId && IsRaceLord(race, stack))
            {
                result.Add(stack);
            }
        }

        return result.ToImmutable();
    }

    public static ImmutableArray<WorldItem> GetExportableItems(WorldState state, WorldPlayerId playerId)
    {
        ImmutableArray<WorldItem>.Builder result = ImmutableArray.CreateBuilder<WorldItem>();

        foreach (WorldStack stack in state.Stacks.Values)
        {
            if (stack.OwnerId == playerId)
            {
                result.AddRange(stack.Equipment.Items.Values);
                result.AddRange(stack.Inventory.Items);
            }
        }

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            if (FortificationCalculator.GetOwner(fortification) == playerId)
            {
                result.AddRange(fortification.Inventory.Items);
            }
        }

        return result.ToImmutable();
    }

    public ScenarioLeader ExportLeader(WorldStack stack, IReadOnlyList<WorldItem> items)
    {
        if (items.Count > ItemLimit)
        {
            throw new ArgumentException($"At most {ItemLimit} items can be exported with a leader.");
        }

        (WorldUnit leader, _, _) = stack.GetLeader(definitions);
        ImmutableArray<UnitModifier>.Builder modifiers = ImmutableArray.CreateBuilder<UnitModifier>();

        foreach (UnitModifier modifier in leader.Modifiers)
        {
            if (modifier.Source is UnitModifierSource.Unit or UnitModifierSource.Scenario)
            {
                if (modifier.RemainingTurns is null)
                {
                    modifiers.Add(modifier);
                }
            }
        }

        List<WorldItem> remaining = [.. items];
        LeaderEquipment equipment = LeaderEquipment.Empty;

        foreach ((LeaderEquipmentSlot slot, WorldItem item) in stack.Equipment.Items)
        {
            if (remaining.Remove(item))
            {
                equipment = equipment.With(slot, item);
            }
        }

        return new ScenarioLeader(
            leader with { Modifiers = modifiers.ToImmutable() },
            equipment,
            new WorldInventory([.. remaining]),
            stack.PendingLeaderUpgrades,
            stack.BattlesWon);
    }

    private bool IsRaceLord(WorldRaceDefinition race, WorldStack stack)
    {
        (WorldUnit leader, _, _) = stack.GetLeader(definitions);

        return race.Leaders.Contains(leader.DefinitionId) && !ThiefCalculator.IsThief(stack, definitions);
    }
}
