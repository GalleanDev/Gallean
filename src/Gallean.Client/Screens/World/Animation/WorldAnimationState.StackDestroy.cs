namespace Gallean.Client;

internal abstract partial record WorldAnimationState
{
    public sealed record StackDestroy(WorldPosition Position, UnitDefinitionId LeaderDefinitionId) : WorldAnimationState;
}
