using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldCastSpellCommand(
    WorldSpellDefinitionId SpellId,
    WorldSpellTarget? Target) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayerId playerId = state.TurnInfo.CurrentPlayerId;
        WorldPlayer player = state.CurrentPlayer;

        SpellCalculator.CastInfo castInfo = SpellCalculator.GetSpellCastInfo(SpellId, player, context);
        if (castInfo.Error is not null)
        {
            return WorldCommandResult.Fail(castInfo.Error.Value);
        }

        WorldSpellDefinition definition = context.Definitions.GetSpell(SpellId);
        if (!SpellTargetCalculator.IsTargetValid(state, player, definition, Target))
        {
            return WorldCommandResult.Fail(WorldCommandError.SpellTargetInvalid);
        }

        if (SpellTargetCalculator.IsTargetConcealed(state, playerId, Target))
        {
            return WorldCommandResult.Fail(WorldCommandError.SpellTargetConcealed);
        }

        WorldCommandResult checkResult = WorldSpellEffectResolver.Resolve(state, context, playerId, Target, definition);
        if (checkResult.Failed)
        {
            return WorldCommandResult.Fail(checkResult.Error.Value);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldSpellCastEvent(playerId, SpellId, Target, castInfo.Cost, WorldSpellCastSource.Player)]);
    }
}
