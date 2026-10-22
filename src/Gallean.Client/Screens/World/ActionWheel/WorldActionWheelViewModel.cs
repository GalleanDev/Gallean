using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed record WorldActionWheelViewModel(WorldPosition Position, IReadOnlyList<WorldActionWheelItemViewModel> Items)
{
    public bool Equals(WorldActionWheelViewModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Position == other.Position && Items.SequenceEqual(other.Items);
    }

    public static WorldActionWheelViewModel? TryCreate(
        WorldState world,
        WorldStack stack,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts,
        IUnitStatsProvider stats)
    {
        (WorldUnit leader, GridPosition _, UnitDefinition _) = stack.GetLeader(definitions);
        LeaderAbility leaderAbilities = stats.GetWorldUnitAbilities(leader);

        List<WorldActionWheelItemViewModel> items = [];

        if (HasOwnAdjacentStack(world, stack))
        {
            items.Add(new WorldActionWheelItemViewModel(
                Tr("Exchange"),
                IconMoniker.Exchange,
                ItemIconKey: null,
                IsHighlighted: false,
                new WorldStackAction.Exchange(stack.Id)));
        }

        if (stack.PendingLeaderUpgrades > 0)
        {
            items.Add(new WorldActionWheelItemViewModel(
                Tr("Learn a new ability ({0})", stack.PendingLeaderUpgrades),
                IconMoniker.LearnAbility,
                ItemIconKey: null,
                IsHighlighted: true,
                new WorldStackAction.LearnAbility(stack.Id)));
        }

        if (leaderAbilities.HasFlag(LeaderAbility.Rod))
        {
            items.Add(new WorldActionWheelItemViewModel(
                Tr("Plant rod"),
                IconMoniker.PlantRod,
                ItemIconKey: null,
                IsHighlighted: false,
                new WorldStackAction.CreateRod(stack.Id)));
            items.Add(new WorldActionWheelItemViewModel(
                Tr("Destroy rod"),
                IconMoniker.DestroyRod,
                ItemIconKey: null,
                IsHighlighted: false,
                new WorldStackAction.DestroyRod(stack.Id)));
        }

        foreach ((LeaderEquipmentSlot _, WorldItem item) in stack.Equipment.Items)
        {
            ItemDefinition definition = definitions.GetItem(item.DefinitionId);

            if (definition.Effect is SpellItemEffect)
            {
                string name = ItemFormatter.FormatName(definitions, texts, item.DefinitionId, item.Charges);

                items.Add(new WorldActionWheelItemViewModel(
                    Tr("Use {0}", name),
                    Icon: null,
                    ItemIconResolver.Resolve(definitions, item.DefinitionId),
                    IsHighlighted: false,
                    new WorldStackAction.UseItem(stack.Id, item.Id)));
            }
        }

        if (items.Count > 0)
        {
            return new WorldActionWheelViewModel(stack.Position, items);
        }
        else
        {
            return null;
        }
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Position);
        hash.AddSequence(Items);

        return hash.ToHashCode();
    }

    private static bool HasOwnAdjacentStack(WorldState world, WorldStack stack)
    {
        foreach (WorldStack other in world.Stacks.Values)
        {
            if (other.Id != stack.Id && other.OwnerId == stack.OwnerId && stack.Position.IsAdjacentTo(other.Position))
            {
                return true;
            }
        }

        return false;
    }
}
