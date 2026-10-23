using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal sealed class UnfogSpellResolver : IWorldSpellResolver<WorldUnfogSpellEffect>, IStaticInstance<UnfogSpellResolver>
{
    public WorldCommandResult Resolve(
        WorldState state,
        IEngineContext context,
        WorldPlayerId? casterId,
        WorldSpellTarget? target,
        WorldSpellDefinition spell,
        WorldUnfogSpellEffect effect)
    {
        if (casterId is null)
        {
            return WorldCommandResult.Fail(WorldCommandError.SpellTargetInvalid);
        }

        WorldPlayer player = state.Players[casterId.Value];
        ImmutableArray<WorldPosition>.Builder revealed = ImmutableArray.CreateBuilder<WorldPosition>();

        foreach (WorldPosition tile in SpellTargetCalculator.GetArea(state, target?.Center, spell.AreaRadius))
        {
            if (!player.FogOfWar.Get(tile.X, tile.Y))
            {
                revealed.Add(tile);
            }
        }

        if (revealed.Count > 0)
        {
            return WorldCommandResult.Success(
                Empty.Value,
                [new WorldFogRevealedEvent(casterId.Value, revealed.ToImmutable())]);
        }
        else
        {
            return WorldCommandResult.Fail(WorldCommandError.SpellTargetInvalid);
        }
    }
}
