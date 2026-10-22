using Gallean.Common;

namespace Gallean.Engine;

internal static class SpyCalculator
{
    private const int ChanceMax = 100;

    public static bool SpiesOnPlayer(WorldState state, WorldPlayerId viewerId, WorldPlayerId ownerId)
    {
        if (viewerId == ownerId || state.Players[ownerId].Race == WorldRace.Neutral)
        {
            return true;
        }
        else
        {
            return state.Players[viewerId].SpiedPlayers.Contains(ownerId);
        }
    }

    public static bool SpiesOnRuin(WorldState state, WorldPlayerId viewerId, WorldFortificationId ruinId)
    {
        return state.Players[viewerId].SpiedRuins.Contains(ruinId);
    }

    public static bool RollSpyDiscover(IRandomProvider random, GlobalVariables globals)
    {
        return random.Next(ChanceMax) < globals.SpyDiscoverChance;
    }
}
