using System.Collections.Immutable;

namespace Gallean.Engine;

internal static class ScenarioStackReferenceCalculator
{
    public static ImmutableArray<WorldStackId> GetLiveInstances(ScenarioStackReference reference, WorldState state)
    {
        ImmutableArray<WorldStackId>.Builder result = ImmutableArray.CreateBuilder<WorldStackId>();

        foreach (WorldStack stack in state.Stacks.Values)
        {
            if (Matches(reference, stack.Id, stack.SourceTemplateId))
            {
                result.Add(stack.Id);
            }
        }

        return result.ToImmutable();
    }

    public static bool IsInstance(ScenarioStackReference reference, WorldStackId stackId, WorldState state)
    {
        if (state.Stacks.TryGetValue(stackId, out WorldStack? stack))
        {
            return Matches(reference, stack.Id, stack.SourceTemplateId);
        }
        else
        {
            return false;
        }
    }

    public static bool IsDestroyed(ScenarioStackReference reference, ImmutableArray<WorldRace> affectedRaces, WorldState state)
    {
        foreach (ScenarioDestroyedStack stack in state.ScenarioState.DestroyedStacks)
        {
            if (Matches(reference, stack.StackId, stack.SourceTemplateId) &&
                affectedRaces.Contains(GetDestroyerRace(stack, state)))
            {
                return true;
            }
        }

        return false;
    }

    public static bool CheckExistence(ScenarioStackReference reference, bool mustExist, WorldState state)
    {
        foreach (WorldStack stack in state.Stacks.Values)
        {
            if (Matches(reference, stack.Id, stack.SourceTemplateId))
            {
                return mustExist;
            }
        }

        return !mustExist;
    }

    private static bool Matches(ScenarioStackReference reference, WorldStackId stackId, ScenarioStackTemplateId? sourceTemplateId)
    {
        return reference switch
        {
            ScenarioStackReference.Stack stack => stack.StackId == stackId,
            ScenarioStackReference.Template template => template.TemplateId == sourceTemplateId,
            _ => throw new ArgumentOutOfRangeException(nameof(reference), reference, null)
        };
    }

    private static WorldRace GetDestroyerRace(ScenarioDestroyedStack stack, WorldState state)
    {
        if (stack.DestroyerPlayerId is not null)
        {
            WorldPlayer player = state.Players[stack.DestroyerPlayerId.Value];

            return player.Race;
        }
        else
        {
            return WorldRace.Neutral;
        }
    }
}
