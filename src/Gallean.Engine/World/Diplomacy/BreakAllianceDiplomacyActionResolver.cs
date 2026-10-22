using Gallean.Common;

namespace Gallean.Engine;

internal sealed class BreakAllianceDiplomacyActionResolver :
    IWorldDiplomacyActionResolver,
    IStaticInstance<BreakAllianceDiplomacyActionResolver>
{
    public WorldCommandResult Resolve(WorldState state, IEngineContext context, WorldPlayerId player, WorldPlayerId targetPlayer)
    {
        WorldRelation relation = DiplomacyCalculator.GetRelation(state, player, targetPlayer);

        if (!relation.Alliance)
        {
            return WorldCommandResult.Fail(WorldCommandError.DiplomacyAllianceRequired);
        }

        if (relation.PermanentAlliance && state.Players[player].IsAi)
        {
            return WorldCommandResult.Fail(WorldCommandError.DiplomacyAllianceBreakForbidden);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldAllianceEndedEvent(player, targetPlayer)]);
    }
}
