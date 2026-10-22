using Gallean.Common;

namespace Gallean.Engine;

internal static class ScenarioEventTriggererCalculator
{
    private sealed class StackResolver :
        IScenarioEventTriggerVisitor<WorldState, WorldStack?>,
        IStaticInstance<StackResolver>
    {
        public WorldStack? Visit(ScenarioEventTurnStartedTrigger trigger, WorldState state)
        {
            return null;
        }

        public WorldStack? Visit(ScenarioEventStackMovedTrigger trigger, WorldState state)
        {
            return state.Stacks.GetValueOrDefault(trigger.StackId);
        }

        public WorldStack? Visit(ScenarioEventStackCreatedTrigger trigger, WorldState state)
        {
            return state.Stacks.GetValueOrDefault(trigger.StackId);
        }

        public WorldStack? Visit(ScenarioEventFortificationCapturedTrigger trigger, WorldState state)
        {
            return state.Stacks.GetValueOrDefault(trigger.CapturingStackId);
        }

        public WorldStack? Visit(ScenarioEventStackDestroyedTrigger trigger, WorldState state)
        {
            return trigger.Destroyer switch
            {
                WorldUnitGroup.Stack data => state.Stacks.GetValueOrDefault(data.StackId),
                WorldUnitGroup.Fortification => null,
                null => null,
                _ => throw new ArgumentOutOfRangeException(nameof(trigger))
            };
        }

        public WorldStack? Visit(ScenarioEventItemGainedTrigger trigger, WorldState state)
        {
            return trigger.Receiver switch
            {
                WorldUnitGroup.Stack data => state.Stacks.GetValueOrDefault(data.StackId),
                WorldUnitGroup.Fortification => null,
                _ => throw new ArgumentOutOfRangeException(nameof(trigger))
            };
        }
    }

    public static WorldStack? FindStack(ScenarioEventTrigger trigger, WorldState state)
    {
        return trigger.Accept(StackResolver.Instance, state);
    }
}
