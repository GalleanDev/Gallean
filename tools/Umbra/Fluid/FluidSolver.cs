namespace Umbra;

internal sealed class FluidSolver(
    UmbraFluidConfiguration configuration,
    FluidControl control,
    int outputWidth,
    int outputHeight)
{
    private const float Epsilon = 1e-4f;
    private const int PhaseCount = 2;
    private const int GustOctaves = 2;
    private const int DetailOctaves = 5;
    private const int GustSeedOffset = 5;
    private const int DetailSeedOffset = 31;

    private readonly double pixelsPerCell = outputWidth / (double)control.Width;
    private readonly int width = control.Width;
    private readonly int height = control.Height;
    private readonly int cells = control.Width * control.Height;
    private readonly FluidField field = new(control.Width, control.Height);
    private readonly float[][] phaseU = new float[PhaseCount][];
    private readonly float[][] phaseV = new float[PhaseCount][];
    private readonly int[] phaseAge = new int[PhaseCount];

    private float[] density = new float[control.Width * control.Height];
    private float[] scratch = new float[control.Width * control.Height];

    public UmbraFluidLoop Solve()
    {
        Reset();

        for (int step = 0; step < configuration.WarmUpSteps; step++)
        {
            Step(step);
        }

        int captureCount = configuration.FrameCount * 2;
        List<byte[]> captured = new(captureCount);

        for (int step = 0; step < captureCount; step++)
        {
            Step(configuration.WarmUpSteps + step);
            captured.Add(Capture());
        }

        return new UmbraFluidLoop(outputWidth, outputHeight, CloseLoop(captured));
    }

    private void Reset()
    {
        for (int index = 0; index < cells; index++)
        {
            density[index] = control.Source[index] * 0.5f;
        }

        for (int phase = 0; phase < PhaseCount; phase++)
        {
            phaseU[phase] = new float[cells];
            phaseV[phase] = new float[cells];
            phaseAge[phase] = phase * configuration.Medium.PhaseLength / PhaseCount;

            SetIdentity(phase);
        }
    }

    private void SetIdentity(int phase)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;

                phaseU[phase][index] = x;
                phaseV[phase][index] = y;
            }
        }
    }

    private void Step(int step)
    {
        AddForces(step);

        field.Confine((float)configuration.Medium.Vorticity);
        field.DiffuseVelocity((float)configuration.Medium.Viscosity);
        field.Limit((float)(configuration.Medium.SpeedLimit / pixelsPerCell));
        field.Project(configuration.Medium.PressureIterations, (float)configuration.Medium.PressureDecay);
        field.AdvectVelocity((float)configuration.Medium.VelocityDissipation);
        field.Advect(density, scratch, (float)configuration.Medium.DensityDissipation);
        (density, scratch) = (scratch, density);

        field.Diffuse(density, scratch, (float)configuration.Medium.DensitySpread);
        (density, scratch) = (scratch, density);

        Replenish();
        AdvancePhases();
    }

    private void AddForces(int step)
    {
        float driftX = (float)(configuration.Medium.DriftX / pixelsPerCell);
        float driftY = (float)(configuration.Medium.DriftY / pixelsPerCell);
        float gravityX = (float)(configuration.Medium.GravityX / pixelsPerCell);
        float gravityY = (float)(configuration.Medium.GravityY / pixelsPerCell);
        float gust = (float)(configuration.Medium.Gust / pixelsPerCell);
        float restoreForce = (float)(configuration.Medium.RestoreForce / pixelsPerCell);
        float current = (float)(configuration.Medium.Current / pixelsPerCell);
        double gustScale = configuration.Medium.GustScale / width;
        double gustOffset = step * configuration.Medium.GustRate;
        float[] velocityU = field.VelocityU;
        float[] velocityV = field.VelocityV;

        Parallel.For(0, height, (int y) =>
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                float turbulence = FluidNoise.Fbm(x * gustScale + gustOffset, y * gustScale, GustOctaves, configuration.Seed + GustSeedOffset) - 0.5f;
                float carried = density[index];
                float modulated = carried * (1 + turbulence);

                velocityU[index] += (driftX + turbulence * gust) * carried + gravityX * modulated + restoreForce * control.RestoreX[index] + current * control.CurrentX[index];
                velocityV[index] += driftY * carried + gravityY * modulated + restoreForce * control.RestoreY[index] + current * control.CurrentY[index];
            }
        });
    }

    private void Replenish()
    {
        float rate = (float)configuration.Medium.SourceRate;
        float limit = (float)configuration.Medium.DensityLimit;

        for (int index = 0; index < cells; index++)
        {
            float carried = Math.Max(density[index], control.Source[index] * rate);

            density[index] = Math.Clamp(carried, 0, limit);
        }
    }

    private void AdvancePhases()
    {
        for (int phase = 0; phase < PhaseCount; phase++)
        {
            field.Advect(phaseU[phase], scratch, 0);
            (phaseU[phase], scratch) = (scratch, phaseU[phase]);

            field.Advect(phaseV[phase], scratch, 0);
            (phaseV[phase], scratch) = (scratch, phaseV[phase]);

            phaseAge[phase]++;

            if (phaseAge[phase] >= configuration.Medium.PhaseLength)
            {
                phaseAge[phase] = 0;
                SetIdentity(phase);
            }
        }
    }

    private byte[] Capture()
    {
        byte[] frame = new byte[outputWidth * outputHeight];
        double grain = configuration.Medium.Grain / width;
        double tail = configuration.Medium.Tail;
        float floor = (float)configuration.Medium.DetailFloor;
        float range = (float)configuration.Medium.DetailRange;
        float scaleX = (width - 1) / (float)Math.Max(outputWidth - 1, 1);
        float scaleY = (height - 1) / (float)Math.Max(outputHeight - 1, 1);

        Parallel.For(0, outputHeight, (int y) =>
        {
            float sourceY = y * scaleY;

            for (int x = 0; x < outputWidth; x++)
            {
                float sourceX = x * scaleX;
                float carried = Sample(density, sourceX, sourceY);
                float detail = 0;
                float total = 0;

                for (int phase = 0; phase < PhaseCount; phase++)
                {
                    float share = 1 - Math.Abs(2.0f * phaseAge[phase] / configuration.Medium.PhaseLength - 1);
                    float u = Sample(phaseU[phase], sourceX, sourceY);
                    float v = Sample(phaseV[phase], sourceX, sourceY);

                    detail += share * FluidNoise.Fbm(u * grain, v * grain, DetailOctaves, configuration.Seed + DetailSeedOffset);
                    total += share;
                }

                detail /= Math.Max(total, Epsilon);

                double shaped = carried * Math.Pow(floor + range * detail, tail);

                frame[y * outputWidth + x] = (byte)Math.Clamp(shaped * byte.MaxValue, byte.MinValue, byte.MaxValue);
            }
        });

        return frame;
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

    private IReadOnlyList<byte[]> CloseLoop(IReadOnlyList<byte[]> captured)
    {
        List<byte[]> frames = new(configuration.FrameCount);

        for (int index = 0; index < configuration.FrameCount; index++)
        {
            float weight = 1 - (float)index / configuration.FrameCount;

            frames.Add(Blend(captured[index], captured[index + configuration.FrameCount], weight));
        }

        return frames;
    }

    private byte[] Blend(byte[] first, byte[] second, float weight)
    {
        byte[] frame = new byte[first.Length];

        Parallel.For(0, first.Length, (int index) =>
        {
            frame[index] = (byte)Math.Clamp(float.Lerp(first[index], second[index], weight), byte.MinValue, byte.MaxValue);
        });

        return frame;
    }
}
