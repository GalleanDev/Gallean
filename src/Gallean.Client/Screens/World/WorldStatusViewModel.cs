using Gallean.Engine;

namespace Gallean.Client;

internal sealed record WorldStatusViewModel(
    WorldPlayerId PlayerId,
    string TurnText,
    AssetSpriteKey.LordFace PortraitKey,
    ResourceSetViewModel Resources,
    WorldIncomeViewModel Income)
{
    public static WorldStatusViewModel Create(
        WorldState state,
        WorldPlayerId playerId,
        WorldGetPlayerInfoCommand.Result playerInfo,
        WorldStatusViewModel? previous)
    {
        WorldPlayer player = state.Players[playerId];

        return new WorldStatusViewModel(
            playerId,
            Tr("Turn {0}", state.TurnInfo.TurnNumber),
            new AssetSpriteKey.LordFace(player.Race, player.FaceVariant.Value, false),
            CreateResources(player, previous),
            new WorldIncomeViewModel(playerInfo));
    }

    private static ResourceSetViewModel CreateResources(WorldPlayer player, WorldStatusViewModel? previous)
    {
        if (previous is not null && previous.PlayerId == player.Id)
        {
            return previous.Resources.Update(player.Resources);
        }
        else
        {
            return new ResourceSetViewModel(player.Resources, ResourceSetProjection.All);
        }
    }
}
