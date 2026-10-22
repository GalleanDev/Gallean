namespace Gallean.Engine;

internal sealed partial class WorldAiOrderPlanner(WorldAiContext context) :
    IWorldStackOrderVisitor<WorldAiOrderPlanner.OrderContext, WorldStackOrder>
{
    private WorldAiContext Context { get; } = context;

    public WorldStackOrder Decide(WorldState state, WorldStack stack, WorldStackOrder order)
    {
        return order.Accept(this, new OrderContext(state, stack));
    }
}
