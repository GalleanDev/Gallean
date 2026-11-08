using System.Collections.Immutable;

namespace Gallean.Engine;

public sealed class ScenarioExporter(IGameDefinitionProvider definitions, IGameTextProvider texts, GlobalVariables globals)
{
    private sealed class ExportedNameVisitor(IGameTextProvider texts) : IUnitNameVisitor<UnitName>
    {
        public UnitName Visit(LiteralUnitName name)
        {
            return name;
        }

        public UnitName Visit(ScenarioUnitName name)
        {
            return new LiteralUnitName(texts.GetScenarioText(name.Id));
        }

        public UnitName Visit(LeaderUnitName name)
        {
            return name;
        }
    }

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

        return BuildLeader(stack, modifier => modifier.Source is UnitModifierSource.Unit or UnitModifierSource.Scenario, items);
    }

    public ScenarioLeader ExportLeader(WorldStack stack, WorldPlayer owner)
    {
        ImmutableArray<LeaderUpgradeDefinition> upgrades = definitions.GetLeaderUpgrades(owner.LordId);

        return BuildLeader(stack, modifier => IsUpgrade(modifier, upgrades), [.. stack.Equipment.Items.Values]);
    }

    private ScenarioLeader BuildLeader(WorldStack stack, Func<UnitModifier, bool> filterModifierFunc, IReadOnlyList<WorldItem> items)
    {
        (WorldUnit leader, _, _) = stack.GetLeader(definitions);
        ImmutableArray<UnitModifier>.Builder modifiers = ImmutableArray.CreateBuilder<UnitModifier>();

        foreach (UnitModifier modifier in leader.Modifiers)
        {
            if (modifier.RemainingTurns is null && filterModifierFunc(modifier))
            {
                modifiers.Add(modifier);
            }
        }

        List<WorldItem> remaining = [.. items];
        LeaderEquipment equipment = LeaderEquipment.Empty;
        int itemIndex = 0;

        foreach ((LeaderEquipmentSlot slot, WorldItem item) in stack.Equipment.Items)
        {
            if (remaining.Remove(item))
            {
                itemIndex++;
                equipment = equipment.With(slot, item with { Id = GetExportItemId(itemIndex) });
            }
        }

        ImmutableArray<WorldItem>.Builder inventory = ImmutableArray.CreateBuilder<WorldItem>();

        foreach (WorldItem item in remaining)
        {
            itemIndex++;
            inventory.Add(item with { Id = GetExportItemId(itemIndex) });
        }

        UnitName? name = leader.Name?.Accept(new ExportedNameVisitor(texts));

        return new ScenarioLeader(
            leader with { Modifiers = modifiers.ToImmutable(), Name = name },
            equipment,
            new WorldInventory(inventory.ToImmutable()),
            stack.PendingLeaderUpgrades,
            stack.BattlesWon);
    }

    private static bool IsUpgrade(UnitModifier modifier, ImmutableArray<LeaderUpgradeDefinition> upgrades)
    {
        if (modifier.Source == UnitModifierSource.Scenario)
        {
            return true;
        }

        if (modifier.Source != UnitModifierSource.Unit)
        {
            return false;
        }

        foreach (LeaderUpgradeDefinition upgrade in upgrades)
        {
            if (upgrade.ModifierId == modifier.DefinitionId)
            {
                return true;
            }
        }

        return false;
    }

    private static WorldItemId GetExportItemId(int index)
    {
        return new WorldItemId($"Exported{index}");
    }

    private bool IsRaceLord(WorldRaceDefinition race, WorldStack stack)
    {
        (WorldUnit leader, _, _) = stack.GetLeader(definitions);

        return race.Leaders.Contains(leader.DefinitionId) && !ThiefCalculator.IsThief(stack, definitions);
    }
}
