namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private abstract record AbstractEconomyAction(WorldAiContext Context, int Priority) : AbstractAction(Context, Priority)
    {
        protected virtual WorldResourceSet OvercommitLimit => WorldResourceSet.Empty;

        public abstract WorldAiActionPlan? Plan(WorldState state, WorldPlayerId playerId, WorldAiMemory memory);

        protected bool CanOvercommit(WorldPlayer player, WorldResourceSet cost)
        {
            return ResourceSetCalculator.CanAfford(ResourceSetCalculator.Add(player.Resources, OvercommitLimit), cost);
        }
    }
}
