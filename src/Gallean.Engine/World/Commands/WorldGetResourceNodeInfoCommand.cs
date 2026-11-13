using CommandResult = Gallean.Engine.ICommandResult<Gallean.Engine.WorldCommandError, Gallean.Engine.WorldEvent, Gallean.Engine.WorldGetResourceNodeInfoCommand.Result>;

namespace Gallean.Engine;

public sealed record WorldGetResourceNodeInfoCommand(WorldPosition Position) : IWorldCommand<WorldGetResourceNodeInfoCommand.Result>
{
    public sealed record Result(WorldResourceSet Income);

    public CommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlan plan = WorldPlan.For(state);
        WorldResourceNode? node = plan.GetResourceNodeAt(Position);

        if (node is null)
        {
            return CommandResult.Fail(WorldCommandError.ResourceNodeNotFound);
        }

        WorldResourceSet income = ResourceNodeCalculator.GetIncome(node, context.Globals);

        return CommandResult.Success(new Result(income), []);
    }
}
