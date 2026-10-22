namespace Gallean.Engine;

internal static class DiplomacyCalculator
{
    public const int RelationMinimum = 0;
    public const int RelationMaximum = 100;

    public static WorldRelation GetRelation(WorldState state, WorldPlayerId playerId, WorldPlayerId otherPlayerId)
    {
        WorldPlayer player = state.Players[playerId];
        WorldPlayer otherPlayer = state.Players[otherPlayerId];
        WorldRelationId relationId = new(player.Race, otherPlayer.Race);

        if (state.Relations.TryGetValue(relationId, out WorldRelation? relation))
        {
            return relation;
        }
        else
        {
            return new WorldRelation(
                relationId,
                RelationMinimum,
                RelationMinimum,
                Alliance: false,
                AllianceTurn: 0,
                PermanentWar: false,
                PermanentAlliance: false);
        }
    }

    public static bool IsHostile(WorldState state, WorldPlayerId playerId, WorldPlayerId otherPlayerId)
    {
        if (playerId == otherPlayerId)
        {
            return false;
        }
        else
        {
            WorldRelation relation = GetRelation(state, playerId, otherPlayerId);

            return !relation.Alliance;
        }
    }

    public static int GetGiftRelationChange(int gold, GlobalVariables globals)
    {
        return gold / globals.DiplomacyGiftGoldPerRelationChange;
    }

    public static WorldRelationLevel ResolveRelationLevel(int relation, GlobalVariables globals)
    {
        if (relation > globals.DiplomacyNeutralRelation)
        {
            return WorldRelationLevel.Peace;
        }
        else if (relation > globals.DiplomacyWarRelation)
        {
            return WorldRelationLevel.Neutral;
        }
        else
        {
            return WorldRelationLevel.War;
        }
    }
}
