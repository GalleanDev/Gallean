using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal sealed class FogSpellResolver : IWorldSpellResolver<WorldFogSpellEffect>, IStaticInstance<FogSpellResolver>
{
    public WorldCommandResult Resolve(
        WorldState state,
        IEngineContext context,
        WorldPlayerId casterId,
        WorldSpellTarget? target,
        WorldSpellDefinition spell,
        WorldFogSpellEffect effect)
    {
        List<WorldEvent> events = [];

        foreach (WorldPlayer player in state.Players.Values)
        {
            if (DiplomacyCalculator.IsHostile(state, casterId, player.Id))
            {
                ImmutableArray<WorldPosition>.Builder positions = ImmutableArray.CreateBuilder<WorldPosition>();

                foreach (WorldPosition tile in SpellTargetCalculator.GetArea(state, target?.Center, spell.AreaRadius))
                {
                    if (player.FogOfWar.Get(tile.X, tile.Y))
                    {
                        positions.Add(tile);
                    }
                }

                if (positions.Count > 0)
                {
                    events.Add(new WorldFogConcealedEvent(player.Id, positions.ToImmutable()));
                }
            }
        }

        if (events.Count > 0)
        {
            return WorldCommandResult.Success(Empty.Value, events);
        }
        else
        {
            return WorldCommandResult.Fail(WorldCommandError.SpellTargetInvalid);
        }
    }
}
