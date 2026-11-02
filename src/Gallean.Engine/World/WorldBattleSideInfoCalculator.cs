namespace Gallean.Engine;

internal static class WorldBattleSideInfoCalculator
{
    public static BattleSideInfo GetInfo(WorldState state, WorldPlayerId? ownerId)
    {
        bool isNeutral = ownerId is null || state.Players[ownerId.Value].Race == WorldRace.Neutral;

        return new BattleSideInfo(isNeutral);
    }
}
