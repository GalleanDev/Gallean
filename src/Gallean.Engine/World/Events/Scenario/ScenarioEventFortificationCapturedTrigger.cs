namespace Gallean.Engine;

public sealed record ScenarioEventFortificationCapturedTrigger(
    WorldFortificationId FortificationId,
    WorldStackId CapturingStackId) : ScenarioEventTrigger;
