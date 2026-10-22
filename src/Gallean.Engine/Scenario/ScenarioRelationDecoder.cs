namespace Gallean.Engine;

internal static class ScenarioRelationDecoder
{
    public static WorldRelation DecodeRelation(WorldRelationId relationId, int value)
    {
        int alliance = (value >> 0) & 0b1;
        int current = (value >> 1) & 0b1111111;
        int previous = (value >> 8) & 0b1111111;
        int allianceTurn = (value >> 15) & 0b111111111111111;
        int permanentWar = (value >> 30) & 0b1;
        int permanentAlliance = (value >> 31) & 0b1;

        return new WorldRelation(
            relationId,
            current,
            previous,
            alliance != 0,
            allianceTurn,
            permanentWar != 0,
            permanentAlliance != 0);
    }
}
