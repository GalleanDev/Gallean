namespace Gallean.Client;

internal readonly record struct BattleGridFootprint(BattleSide Side, GridPosition Position, bool IsLarge)
{
    public IEnumerable<BattleGridTile> EnumerateTiles()
    {
        if (IsLarge)
        {
            yield return new BattleGridTile(Side, GridPosition.AtFrontLine(Position.Flank));
            yield return new BattleGridTile(Side, GridPosition.AtBackLine(Position.Flank));
        }
        else
        {
            yield return new BattleGridTile(Side, Position);
        }
    }
}
