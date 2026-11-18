using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldRenameLeaderCommand(
    WorldStackId StackId,
    string Name) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
        }

        string name = Name.Trim();

        if (name.Length <= 0)
        {
            return WorldCommandResult.Fail(WorldCommandError.LeaderNameEmpty);
        }

        if (name.Length > context.Globals.MaxLeaderNameLength)
        {
            return WorldCommandResult.Fail(WorldCommandError.LeaderNameTooLong);
        }

        (WorldUnit leader, _, _) = stack.GetLeader(context.Definitions);

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldLeaderRenamedEvent(StackId, leader.Id, new LiteralUnitName(name))]);
    }
}
