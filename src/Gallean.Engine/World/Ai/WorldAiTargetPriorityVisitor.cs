namespace Gallean.Engine;

internal sealed class WorldAiTargetPriorityVisitor
    : IWorldAiTargetVisitor<WorldAiTargetPriorityVisitor.Context, int>
{
    internal readonly record struct Context(WorldState State);

    public int Visit(WorldAiTarget.Stack target, Context context)
    {
        WorldStack stack = context.State.Stacks[target.StackId];

        return stack.AiPriority;
    }

    public int Visit(WorldAiTarget.Fortification target, Context context)
    {
        WorldFortification fortification = context.State.Fortifications[target.FortificationId];

        return fortification.AiPriority;
    }

    public int Visit(WorldAiTarget.Location target, Context context)
    {
        throw new InvalidOperationException("Invalid world AI target type.");
    }

    public int Visit(WorldAiTarget.Bag target, Context context)
    {
        WorldBag bag = context.State.Bags[target.BagId];

        return bag.AiPriority;
    }
}
