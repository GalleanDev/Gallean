namespace Gallean.Client;

internal static class BattleRenderDepth
{
    private const int LayerCount = 2;
    private const int UnitLayer = 0;
    private const int EffectLayer = 1;

    public static int GetUnitDepth(BattleGridFootprint footprint)
    {
        return GetTileDepth(footprint) + UnitLayer;
    }

    public static int GetEffectDepth(BattleGridFootprint footprint)
    {
        return GetTileDepth(footprint) + EffectLayer;
    }

    private static int GetTileDepth(BattleGridFootprint footprint)
    {
        int side = GetSideOrder(footprint.Side);
        int flank = GetFlankOrder(footprint.Position);
        int line = GetLineOrder(footprint);

        return ((side * GridPosition.FlankCount + flank) * GridPosition.LineCount + line) * LayerCount;
    }

    private static int GetSideOrder(BattleSide side)
    {
        if (side == BattleSide.Attacker)
        {
            return 0;
        }
        else
        {
            return 1;
        }
    }

    private static int GetFlankOrder(GridPosition position)
    {
        return GridPosition.FlankCount - 1 - position.Flank;
    }

    private static int GetLineOrder(BattleGridFootprint footprint)
    {
        if (IsNearViewer(footprint))
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }

    private static bool IsNearViewer(BattleGridFootprint footprint)
    {
        if (footprint.Side == BattleSide.Attacker)
        {
            return footprint.Position.IsFrontLine;
        }
        else
        {
            return footprint.Position.IsBackLine;
        }
    }
}
