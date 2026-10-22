using Gallean.Common;

namespace Gallean.Engine;

internal sealed record WorldAiMoveStackCommand(WorldStackId StackId, WorldPosition TargetPosition) : AbstractWorldMoveStackCommand(StackId, TargetPosition)
{
    private protected override ImmutableBitmap? GetFogOfWar(WorldState state, WorldStack stack)
    {
        return stack.Order.Accept(WorldAiFogOfWarVisitor.Instance, state.Players[stack.OwnerId]);
    }
}
