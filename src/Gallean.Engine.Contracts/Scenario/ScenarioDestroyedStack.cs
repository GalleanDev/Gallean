namespace Gallean.Engine.Contracts;

public readonly record struct ScenarioDestroyedStack(
    WorldStackId StackId,
    WorldPlayerId? DestroyerPlayerId,
    ScenarioStackTemplateId? SourceTemplateId);
