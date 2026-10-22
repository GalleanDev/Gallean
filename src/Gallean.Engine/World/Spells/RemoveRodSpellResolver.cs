using Gallean.Common;

namespace Gallean.Engine;

internal sealed class RemoveRodSpellResolver : IWorldSpellResolver<WorldRemoveRodSpellEffect>, IStaticInstance<RemoveRodSpellResolver>
{
    public WorldCommandResult Resolve(
        WorldState state,
        IEngineContext context,
        WorldPlayerId casterId,
        WorldSpellTarget? target,
        WorldSpellDefinition spell,
        WorldRemoveRodSpellEffect effect)
    {
        WorldPlan plan = WorldPlan.For(state);
        List<WorldRod> rods = [];

        foreach (WorldPosition tile in SpellTargetCalculator.GetArea(state, target?.Center, spell.AreaRadius))
        {
            if (plan.GetRodAt(tile) is { } rod)
            {
                rods.Add(rod);
            }
        }

        if (rods.Count > 0)
        {
            List<WorldEvent> events = [];

            foreach (WorldRod rod in rods)
            {
                events.Add(new WorldRodDestroyedEvent(rod.Id, casterId, rod.OwnerId, rod.Position));
            }

            return WorldCommandResult.Success(Empty.Value, events);
        }
        else
        {
            return WorldCommandResult.Fail(WorldCommandError.RodNotFound);
        }
    }
}
