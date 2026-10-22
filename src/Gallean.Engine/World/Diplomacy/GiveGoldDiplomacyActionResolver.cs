using Gallean.Common;

namespace Gallean.Engine;

internal sealed class GiveGoldDiplomacyActionResolver(WorldDiplomacyGiveGoldAction action) : IWorldDiplomacyActionResolver
{
    public WorldCommandResult Resolve(WorldState state, IEngineContext context, WorldPlayerId player, WorldPlayerId targetPlayer)
    {
        if (action.Gold <= 0)
        {
            return WorldCommandResult.Fail(WorldCommandError.DiplomacyGiftInvalid);
        }

        WorldPlayer giver = state.Players[player];

        if (giver.Resources.Gold < action.Gold)
        {
            return WorldCommandResult.Fail(WorldCommandError.InsufficientResources);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldGoldGivenEvent(player, targetPlayer, action.Gold)]);
    }
}
