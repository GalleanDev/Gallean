using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

public sealed record WorldScenarioState(
    ImmutableSortedDictionary<ScenarioEventId, bool> EventStates,
    ImmutableArray<ScenarioDestroyedStack> DestroyedStacks,
    ImmutableSortedDictionary<ScenarioVariableId, int> Variables,
    ScenarioTextId? ObjectiveTextId,
    bool IsEnded,
    WorldPlayerId? Winner,
    ImmutableArray<WorldScenarioMessage> Messages = default)
{
    public static WorldScenarioState Empty { get; } = new(
        ImmutableSortedDictionary<ScenarioEventId, bool>.Empty,
        [],
        ImmutableSortedDictionary<ScenarioVariableId, int>.Empty,
        null,
        false,
        null,
        []);

    public ImmutableArray<WorldScenarioMessage> Messages { get; init; } = Messages.IsDefault ? [] : Messages;

    public bool Equals(WorldScenarioState? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return EventStates.SequenceEqual(other.EventStates) &&
               Messages.SequenceEqual(other.Messages) &&
               DestroyedStacks.SequenceEqual(other.DestroyedStacks) &&
               Variables.SequenceEqual(other.Variables) &&
               ObjectiveTextId == other.ObjectiveTextId &&
               IsEnded == other.IsEnded &&
               Winner == other.Winner;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.AddSequence(EventStates);
        hash.AddSequence(Messages);
        hash.AddSequence(DestroyedStacks);
        hash.AddSequence(Variables);
        hash.Add(ObjectiveTextId);
        hash.Add(IsEnded);
        hash.Add(Winner);

        return hash.ToHashCode();
    }
}
