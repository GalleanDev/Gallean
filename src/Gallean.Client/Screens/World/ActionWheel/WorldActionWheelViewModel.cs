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
        IUnitStatsProvider stats)
    {
        WorldPlan plan = WorldPlan.For(world);
        (WorldUnit leader, GridPosition _, UnitDefinition _) = stack.GetLeader(definitions);
        LeaderAbility leaderAbilities = stats.GetWorldUnitAbilities(leader);

        List<WorldActionWheelItemViewModel> items = [];

        if (HasOwnAdjacentStack(world, stack))
        {
            items.Add(new WorldActionWheelItemViewModel(
                Tr("Exchange"),
                IconMoniker.Exchange,
                IsHighlighted: false,
                new WorldStackAction.Exchange(stack.Id)));
        }

        if (stack.PendingLeaderUpgrades > 0)
        {
            items.Add(new WorldActionWheelItemViewModel(
                Tr("Learn a new ability ({0})", stack.PendingLeaderUpgrades),
                IconMoniker.LearnAbility,
                IsHighlighted: true,
                new WorldStackAction.LearnAbility(stack.Id)));
        }

        WorldFortification? fortification = plan.GetFortificationAt(stack.Position);

        if (leaderAbilities.HasFlag(LeaderAbility.Rod) && fortification is not (WorldCity or WorldCapital))
        {
            items.Add(new WorldActionWheelItemViewModel(
                Tr("Plant rod"),
                IconMoniker.PlantRod,
                IsHighlighted: false,
                new WorldStackAction.CreateRod(stack.Id)));
            items.Add(new WorldActionWheelItemViewModel(
                Tr("Destroy rod"),
                IconMoniker.DestroyRod,
                IsHighlighted: false,
                new WorldStackAction.DestroyRod(stack.Id)));
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
