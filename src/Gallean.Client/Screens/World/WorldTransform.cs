using Avalonia;

namespace Gallean.Client;

internal static class WorldTransform
{
    public const int TileHalfWidth = 32;
    public const int TileHalfHeight = 16;
    public const int TileWidth = TileHalfWidth * 2;
    public const int TileHeight = TileHalfHeight * 2;

    public static Point ToWorldPoint(WorldPosition position)
    {
        return ToWorldPoint(position.X, position.Y);
    }

    public static Point ToWorldPoint(double x, double y)
    {
        return new Point((x - y) * TileHalfWidth, (x + y) * TileHalfHeight);
    }

    public static WorldPosition FromWorldPoint(Point point)
    {
        double diagonalA = point.Y / TileHalfHeight;
        double diagonalB = point.X / TileHalfWidth;
        int x = (int)Math.Floor((diagonalA + diagonalB) / 2.0 + 0.5);
        int y = (int)Math.Floor((diagonalA - diagonalB) / 2.0 + 0.5);

        return new WorldPosition(x, y);
    }

    public static Point ToFractionalCell(Point point)
    {
        double diagonalA = point.Y / TileHalfHeight;
        double diagonalB = point.X / TileHalfWidth;

        return new Point((diagonalA + diagonalB) / 2.0, (diagonalA - diagonalB) / 2.0);
    }

    public static Point ClampToMap(Point point, int width, int height)
    {
        if (width > 0 && height > 0)
        {
            Point cell = ToFractionalCell(point);
            double x = Math.Clamp(cell.X, 0, width - 1.0);
            double y = Math.Clamp(cell.Y, 0, height - 1.0);

            return ToWorldPoint(x, y);
        }
        else
        {
            return point;
        }
    }
}
