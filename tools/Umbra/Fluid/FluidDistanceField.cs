using System.Numerics;

namespace Umbra;

internal sealed class FluidDistanceField(bool[] seeds, bool[] mask, int width, int height)
{
    private const float DiagonalCost = 1.41421354f;

    private readonly float[] distances = Measure(seeds, mask, width, height);

    public float GetDistance(int x, int y)
    {
        return distances[y * width + x];
    }

    public Vector2 GetDirection(int x, int y)
    {
        float distance = GetDistance(x, y);

        if (!float.IsFinite(distance))
        {
            return Vector2.Zero;
        }

        float left = GetNeighborDistance(x - 1, y, distance);
        float right = GetNeighborDistance(x + 1, y, distance);
        float top = GetNeighborDistance(x, y - 1, distance);
        float bottom = GetNeighborDistance(x, y + 1, distance);
        Vector2 descent = new(left - right, top - bottom);

        if (descent.LengthSquared() > 0)
        {
            return Vector2.Normalize(descent);
        }
        else
        {
            return Vector2.Zero;
        }
    }

    private static float[] Measure(bool[] seeds, bool[] mask, int width, int height)
    {
        float[] distances = new float[width * height];

        for (int index = 0; index < distances.Length; index++)
        {
            if (seeds[index])
            {
                distances[index] = 0;
            }
            else
            {
                distances[index] = float.PositiveInfinity;
            }
        }

        bool changed = true;

        while (changed)
        {
            changed = false;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    changed |= Relax(x, y, -1, 0, 1);
                    changed |= Relax(x, y, 0, -1, 1);
                    changed |= Relax(x, y, -1, -1, DiagonalCost);
                    changed |= Relax(x, y, 1, -1, DiagonalCost);
                }
            }

            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = width - 1; x >= 0; x--)
                {
                    changed |= Relax(x, y, 1, 0, 1);
                    changed |= Relax(x, y, 0, 1, 1);
                    changed |= Relax(x, y, 1, 1, DiagonalCost);
                    changed |= Relax(x, y, -1, 1, DiagonalCost);
                }
            }
        }

        return distances;

        bool Relax(int x, int y, int offsetX, int offsetY, float cost)
        {
            int neighborX = x + offsetX;
            int neighborY = y + offsetY;

            if (mask[y * width + x] && neighborX >= 0 && neighborX < width && neighborY >= 0 && neighborY < height)
            {
                float candidate = distances[neighborY * width + neighborX] + cost;

                if (candidate < distances[y * width + x])
                {
                    distances[y * width + x] = candidate;

                    return true;
                }
            }

            return false;
        }
    }

    private float GetNeighborDistance(int x, int y, float fallback)
    {
        if (x >= 0 && x < width && y >= 0 && y < height && float.IsFinite(distances[y * width + x]))
        {
            return distances[y * width + x];
        }
        else
        {
            return fallback;
        }
    }
}
