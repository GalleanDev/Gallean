namespace Gallean.Engine.Contracts;

public sealed record GridPosition(int Line, int Flank)
{
    public const int FrontLine = 0;
    public const int BackLine = 1;
    public const int LineCount = 2;
    public const int FlankCount = 3;
    public const int Count = LineCount * FlankCount;

    public static readonly GridPosition FrontBottom = new(FrontLine, 0);
    public static readonly GridPosition FrontCenter = new(FrontLine, 1);
    public static readonly GridPosition FrontTop = new(FrontLine, 2);
    public static readonly GridPosition BackBottom = new(BackLine, 0);
    public static readonly GridPosition BackCenter = new(BackLine, 1);
    public static readonly GridPosition BackTop = new(BackLine, 2);

    public static readonly IReadOnlyList<GridPosition> All = [FrontBottom, FrontCenter, FrontTop, BackBottom, BackCenter, BackTop];

    public bool IsFrontLine => Line == FrontLine;
    public bool IsBackLine => Line == BackLine;

    public int ToIndex()
    {
        return Line * FlankCount + Flank;
    }

    public static GridPosition FromIndex(int index)
    {
        return All[index];
    }

    public static GridPosition AtFrontLine(int flank)
    {
        return All[FrontLine * FlankCount + flank];
    }

    public static GridPosition AtBackLine(int flank)
    {
        return All[BackLine * FlankCount + flank];
    }
}
