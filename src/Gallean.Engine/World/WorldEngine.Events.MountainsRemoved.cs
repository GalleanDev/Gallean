namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldMountainsRemovedEvent ev, WorldState state)
    {
        ScenarioLocationDefinition location = Context.Scenario.GetLocation(ev.Location);

        return ScenarioMountainRemovalCalculator.Remove(state, location, ev.MountainIds);
    }
}
