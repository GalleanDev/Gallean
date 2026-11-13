namespace Gallean.Engine;

internal static class ScenarioEventTriggererCalculator
{
    public static WorldStack? FindStack(ScenarioEventTrigger trigger, WorldState state)
    {
        return trigger switch
        {
            ScenarioEventTurnStartedTrigger => null,
            ScenarioEventStackMovedTrigger stackMoved => state.Stacks.GetValueOrDefault(stackMoved.StackId),
            ScenarioEventStackCreatedTrigger stackCreated => state.Stacks.GetValueOrDefault(stackCreated.StackId),
            ScenarioEventStackDestroyedTrigger stackDestroyed => FindDestroyer(stackDestroyed, state),
            ScenarioEventFortificationCapturedTrigger fortificationCaptured => state.Stacks.GetValueOrDefault(fortificationCaptured.CapturingStackId),
            ScenarioEventItemGainedTrigger itemGained => FindReceiver(itemGained, state)
        };
    }

    private static WorldStack? FindDestroyer(ScenarioEventStackDestroyedTrigger trigger, WorldState state)
    {
        return trigger.Destroyer switch
        {
            WorldUnitGroup.Stack data => state.Stacks.GetValueOrDefault(data.StackId),
            WorldUnitGroup.Fortification => null,
            null => null
        };
    }

    private static WorldStack? FindReceiver(ScenarioEventItemGainedTrigger trigger, WorldState state)
    {
        return trigger.Receiver switch
        {
            WorldUnitGroup.Stack data => state.Stacks.GetValueOrDefault(data.StackId),
            WorldUnitGroup.Fortification => null
        };
    }
}
