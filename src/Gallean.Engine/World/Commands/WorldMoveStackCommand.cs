using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldMoveStackCommand(
    WorldStackId StackId,
    WorldPosition TargetPosition) : AbstractWorldMoveStackCommand(StackId, TargetPosition)
{
    private protected override ImmutableBitmap GetFogOfWar(WorldState state, WorldStack stack)
    {
        return state.Players[stack.OwnerId].FogOfWar;
    }
}
