namespace Gallean.Client;

internal abstract partial record WorldAnimationState
{
    public sealed record StackRemove(WorldPosition Position, UnitDefinitionId LeaderDefinitionId) : WorldAnimationState;
}
