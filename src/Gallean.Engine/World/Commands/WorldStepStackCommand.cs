using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldStepStackCommand(WorldStackId StackId, WorldPosition TargetPosition) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldMoveStackCommand moveCommand = new(StackId, TargetPosition);
        WorldCommandResult moveResult = moveCommand.Execute(state, context);

        if (moveResult.Events is [WorldStackMovingEvent { RemainingPath.IsEmpty: false } moveEvent])
        {
            moveEvent = moveEvent with { RemainingPath = [], EncounterPosition = null };

            return WorldCommandResult.Success(Empty.Value, [moveEvent]);
        }
        else
        {
            return moveResult;
        }
    }
}
