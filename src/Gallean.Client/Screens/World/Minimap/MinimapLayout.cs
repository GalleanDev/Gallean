using Avalonia;

namespace Gallean.Client;

internal readonly record struct MinimapLayout(
    Rect Box,
    int MapWidth,
    int MapHeight,
    double CenterX,
    double Top,
    double HalfWidth,
    double HalfHeight)
{
    public static MinimapLayout Create(int mapWidth, int mapHeight, Rect box)
    {
        int span = Math.Max(1, mapWidth + mapHeight);
        double halfWidth = Math.Max(1, Math.Min(box.Width, box.Height * 2) / (span + 2));
        double halfHeight = Math.Max(0.5, halfWidth / 2.0);
        double centerX = box.X + box.Width / 2.0 - (mapWidth - mapHeight) / 2.0 * halfWidth;
        double top = box.Y + box.Height / 2.0 - (mapWidth + mapHeight - 2) / 2.0 * halfHeight;

        return new MinimapLayout(box, mapWidth, mapHeight, centerX, top, halfWidth, halfHeight);
    }

    public Point CellToScreen(double cellX, double cellY)
    {
        return new Point(CenterX + (cellX - cellY) * HalfWidth, Top + (cellX + cellY) * HalfHeight);
    }

    public WorldPosition ToCell(Point point)
    {
        double diagonalX = (point.X - CenterX) / HalfWidth;
        double diagonalY = (point.Y - Top) / HalfHeight;
        int x = (int)Math.Round((diagonalX + diagonalY) / 2.0);
        int y = (int)Math.Round((diagonalY - diagonalX) / 2.0);

        return new WorldPosition(x, y);
    }

    public bool Contains(Point point)
    {
        WorldPosition cell = ToCell(point);

        return cell.X >= 0 && cell.X < MapWidth && cell.Y >= 0 && cell.Y < MapHeight;
    }
}
