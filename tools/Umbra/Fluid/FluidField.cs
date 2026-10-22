namespace Umbra;

internal sealed class FluidField(int width, int height)
{
    private const float Epsilon = 1e-4f;

    private readonly int cells = width * height;
    private readonly float[] divergence = new float[width * height];
    private readonly float[] vorticity = new float[width * height];
    private readonly float[] strength = new float[width * height];

    private float[] pressure = new float[width * height];
    private float[] relaxed = new float[width * height];
    private float[] scratchU = new float[width * height];
    private float[] scratchV = new float[width * height];

    public float[] VelocityU { get; private set; } = new float[width * height];
    public float[] VelocityV { get; private set; } = new float[width * height];

    public void Advect(float[] source, float[] target, float dissipation)
    {
        float decay = 1 + dissipation;

        Parallel.For(0, height, (int y) =>
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;

                target[index] = Sample(source, x - VelocityU[index], y - VelocityV[index]) / decay;
            }
        });
    }

    public void AdvectVelocity(float dissipation)
    {
        Advect(VelocityU, scratchU, dissipation);
        Advect(VelocityV, scratchV, dissipation);
        (VelocityU, scratchU) = (scratchU, VelocityU);
        (VelocityV, scratchV) = (scratchV, VelocityV);
    }

    public void Confine(float amount)
    {
        Parallel.For(0, height, (int y) =>
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;

                vorticity[index] = 0.5f * (VelocityV[Right(x, y)] - VelocityV[Left(x, y)] - VelocityU[Below(x, y)] + VelocityU[Above(x, y)]);
                strength[index] = Math.Abs(vorticity[index]);
            }
        });

        Parallel.For(0, height, (int y) =>
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                float forceX = 0.5f * (strength[Below(x, y)] - strength[Above(x, y)]);
                float forceY = 0.5f * (strength[Right(x, y)] - strength[Left(x, y)]);
                float length = MathF.Sqrt(forceX * forceX + forceY * forceY) + Epsilon;

                VelocityU[index] += forceX / length * amount * vorticity[index];
                VelocityV[index] -= forceY / length * amount * vorticity[index];
            }
        });
    }

    public void Project(int iterations, float decay)
    {
        Parallel.For(0, height, (int y) =>
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                float left = x > 0 ? VelocityU[Left(x, y)] : -VelocityU[index];
                float right = x < width - 1 ? VelocityU[Right(x, y)] : -VelocityU[index];
                float above = y > 0 ? VelocityV[Above(x, y)] : -VelocityV[index];
                float below = y < height - 1 ? VelocityV[Below(x, y)] : -VelocityV[index];

                divergence[index] = 0.5f * (right - left + below - above);
                pressure[index] *= decay;
            }
        });

        for (int iteration = 0; iteration < iterations; iteration++)
        {
            Parallel.For(0, height, (int y) =>
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    float neighbors = pressure[Left(x, y)] + pressure[Right(x, y)] + pressure[Above(x, y)] + pressure[Below(x, y)];

                    relaxed[index] = (neighbors - divergence[index]) * 0.25f;
                }
            });

            (pressure, relaxed) = (relaxed, pressure);
        }

        Parallel.For(0, height, (int y) =>
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;

                VelocityU[index] -= pressure[Right(x, y)] - pressure[Left(x, y)];
                VelocityV[index] -= pressure[Below(x, y)] - pressure[Above(x, y)];
            }
        });
    }

    public void Diffuse(float[] source, float[] target, float amount)
    {
        Parallel.For(0, height, (int y) =>
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                float neighbors = source[Left(x, y)] + source[Right(x, y)] + source[Above(x, y)] + source[Below(x, y)];

                target[index] = source[index] * (1 - amount) + neighbors * 0.25f * amount;
            }
        });
    }

    public void DiffuseVelocity(float amount)
    {
        Diffuse(VelocityU, scratchU, amount);
        Diffuse(VelocityV, scratchV, amount);
        (VelocityU, scratchU) = (scratchU, VelocityU);
        (VelocityV, scratchV) = (scratchV, VelocityV);
    }

    public void Limit(float limit)
    {
        for (int index = 0; index < cells; index++)
        {
            float speed = MathF.Sqrt(VelocityU[index] * VelocityU[index] + VelocityV[index] * VelocityV[index]);
            float scale = Math.Min(limit / Math.Max(speed, Epsilon), 1);

            VelocityU[index] *= scale;
            VelocityV[index] *= scale;
        }
    }

    private float Sample(float[] field, float u, float v)
    {
        u = Math.Clamp(u, 0, width - 1);
        v = Math.Clamp(v, 0, height - 1);

        int x0 = (int)u;
        int y0 = (int)v;
        int x1 = Math.Min(x0 + 1, width - 1);
        int y1 = Math.Min(y0 + 1, height - 1);
        float fractionX = u - x0;
        float fractionY = v - y0;
        int topRow = y0 * width;
        int bottomRow = y1 * width;

        float top = field[topRow + x0] + (field[topRow + x1] - field[topRow + x0]) * fractionX;
        float bottom = field[bottomRow + x0] + (field[bottomRow + x1] - field[bottomRow + x0]) * fractionX;

        return top + (bottom - top) * fractionY;
    }

    private int Left(int x, int y)
    {
        return y * width + Math.Max(x - 1, 0);
    }

    private int Right(int x, int y)
    {
        return y * width + Math.Min(x + 1, width - 1);
    }

    private int Above(int x, int y)
    {
        return Math.Max(y - 1, 0) * width + x;
    }

    private int Below(int x, int y)
    {
        return Math.Min(y + 1, height - 1) * width + x;
    }
}
