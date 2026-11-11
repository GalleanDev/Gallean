namespace Gallean.Client;

internal closed partial record WorldAnimationState
{
    public sealed record StackRemove(WorldPosition Position, UnitDefinitionId LeaderDefinitionId) : WorldAnimationState;
}
