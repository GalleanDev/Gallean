namespace GameContent;

public readonly record struct Rectangle(int X, int Y, int Width, int Height)
{
    public static readonly Rectangle Empty = new();

    public static Rectangle FromLtrb(int left, int top, int right, int bottom)
    {
        return new Rectangle(left, top, right - left, bottom - top);
    }

    public static Rectangle Union(Rectangle a, Rectangle b)
    {
        int x1 = Math.Min(a.X, b.X);
        int x2 = Math.Max(a.X + a.Width, b.X + b.Width);
        int y1 = Math.Min(a.Y, b.Y);
        int y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);

        return new Rectangle(x1, y1, x2 - x1, y2 - y1);
    }
}
