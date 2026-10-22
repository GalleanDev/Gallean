using Avalonia;

namespace Gallean.Client;

internal static class OrnamentPlacement
{
    public static Matrix At(Point origin, double size)
    {
        return Matrix.CreateScale(size, size) * Matrix.CreateTranslation(origin.X, origin.Y);
    }

    public static Matrix MirrorHorizontally(double center)
    {
        return Matrix.CreateScale(-1, 1) * Matrix.CreateTranslation(2 * center, 0);
    }

    public static Matrix MirrorVertically(double center)
    {
        return Matrix.CreateScale(1, -1) * Matrix.CreateTranslation(0, 2 * center);
    }
}
