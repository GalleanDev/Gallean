namespace Gallean.Client;

internal closed partial record WorldAnimationState
{
    public sealed record StackDestroy(WorldPosition Position, UnitDefinitionId LeaderDefinitionId) : WorldAnimationState;
}
