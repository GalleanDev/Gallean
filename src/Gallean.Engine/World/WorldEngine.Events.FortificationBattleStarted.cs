namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldFortificationBattleStartedEvent ev, WorldState state)
    {
        WorldStack attacker = state.Stacks[ev.AttackerStackId];
        WorldStack updatedAttacker = attacker with
        {
            Direction = StackStepCalculator.GetDirection(attacker.Position, ev.Position)
        };

        return state with { Stacks = state.Stacks.SetItem(ev.AttackerStackId, updatedAttacker) };
    }
}
