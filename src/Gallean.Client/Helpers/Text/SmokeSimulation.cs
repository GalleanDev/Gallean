using Avalonia;

namespace Gallean.Client;

internal sealed class SmokeSimulation
{
    private const int DensityRefinement = 4;
    private const int PressureIterations = 20;
    private const float PressureRetention = 0.8f;
    private const float Vorticity = 3.0f;
    private const float Buoyancy = 38.0f;
    private const float VelocityDissipation = 0.2f;
    private const float DensityDissipation = 0.9f;
    private const float PuffReach = 3.0f;

    private readonly int columns;
    private readonly int rows;
    private readonly float cellSize;
    private readonly float densityCellSize;
    private readonly float[] curl;
    private readonly float[] divergence;
    private float[] velocityX;
    private float[] velocityY;
    private float[] nextVelocityX;
    private float[] nextVelocityY;
    private float[] pressure;
    private float[] nextPressure;
    private float[] density;
    private float[] nextDensity;

    public SmokeSimulation(Size area, double cellSize)
    {
        this.cellSize = (float)cellSize;
        densityCellSize = this.cellSize / DensityRefinement;
        columns = Math.Max(1, (int)Math.Ceiling(area.Width / cellSize));
        rows = Math.Max(1, (int)Math.Ceiling(area.Height / cellSize));
        DensityColumns = columns * DensityRefinement;
        DensityRows = rows * DensityRefinement;

        curl = new float[columns * rows];
        divergence = new float[columns * rows];
        velocityX = new float[columns * rows];
        velocityY = new float[columns * rows];
        nextVelocityX = new float[columns * rows];
        nextVelocityY = new float[columns * rows];
        pressure = new float[columns * rows];
        nextPressure = new float[columns * rows];
        density = new float[DensityColumns * DensityRows];
        nextDensity = new float[DensityColumns * DensityRows];
    }

    public int DensityColumns { get; }
    public int DensityRows { get; }

    public Size Area => new(DensityColumns * densityCellSize, DensityRows * densityCellSize);

    public void AddPuff(Point center, double radius, double amount, Vector drift, double swirl)
    {
        float centerX = (float)center.X;
        float centerY = (float)center.Y;
        float reach = PuffReach * (float)radius;
        float inverseRadiusSquared = 1.0f / (float)(radius * radius);
        float spin = (float)(swirl / radius);
        (int firstDensityColumn, int lastDensityColumn) = GetRange(centerX, reach, densityCellSize, DensityColumns);
        (int firstDensityRow, int lastDensityRow) = GetRange(centerY, reach, densityCellSize, DensityRows);
        (int firstColumn, int lastColumn) = GetRange(centerX, reach, cellSize, columns);
        (int firstRow, int lastRow) = GetRange(centerY, reach, cellSize, rows);

        for (int row = firstDensityRow; row <= lastDensityRow; row++)
        {
            float offsetY = ((row + 0.5f) * densityCellSize) - centerY;

            for (int column = firstDensityColumn; column <= lastDensityColumn; column++)
            {
                float offsetX = ((column + 0.5f) * densityCellSize) - centerX;
                float weight = MathF.Exp(-((offsetX * offsetX) + (offsetY * offsetY)) * inverseRadiusSquared);

                density[(row * DensityColumns) + column] += (float)amount * weight;
            }
        }

        for (int row = firstRow; row <= lastRow; row++)
        {
            float offsetY = ((row + 0.5f) * cellSize) - centerY;

            for (int column = firstColumn; column <= lastColumn; column++)
            {
                float offsetX = ((column + 0.5f) * cellSize) - centerX;
                float weight = MathF.Exp(-((offsetX * offsetX) + (offsetY * offsetY)) * inverseRadiusSquared);
                int index = (row * columns) + column;

                velocityX[index] += weight * ((float)drift.X - (spin * offsetY));
                velocityY[index] += weight * ((float)drift.Y + (spin * offsetX));
            }
        }
    }

    public void Step(TimeSpan delta)
    {
        float seconds = (float)delta.TotalSeconds;

        ComputeCurl();
        ApplyForces(seconds);
        ComputeDivergence();
        SolvePressure();
        SubtractPressureGradient();
        AdvectVelocity(seconds);
        AdvectDensity(seconds);
    }

    public byte[] CreateOpacity(double absorption)
    {
        byte[] pixels = new byte[density.Length];

        for (int index = 0; index < density.Length; index++)
        {
            double opacity = 1 - Math.Exp(-Math.Max(density[index], 0) * absorption);

            pixels[index] = (byte)Math.Round(opacity * byte.MaxValue);
        }

        return pixels;
    }

    private void ComputeCurl()
    {
        float scale = 0.5f / cellSize;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                float right = velocityY[Index(column + 1, row)];
                float left = velocityY[Index(column - 1, row)];
                float below = velocityX[Index(column, row + 1)];
                float above = velocityX[Index(column, row - 1)];

                curl[(row * columns) + column] = scale * ((right - left) - (below - above));
            }
        }
    }

    private void ApplyForces(float seconds)
    {
        float scale = 0.5f / cellSize;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int index = (row * columns) + column;
                float gradientX = scale * (Math.Abs(curl[Index(column + 1, row)]) - Math.Abs(curl[Index(column - 1, row)]));
                float gradientY = scale * (Math.Abs(curl[Index(column, row + 1)]) - Math.Abs(curl[Index(column, row - 1)]));
                float length = MathF.Sqrt((gradientX * gradientX) + (gradientY * gradientY)) + 1e-5f;
                float confinement = Vorticity * cellSize * curl[index] / length;
                float cellDensity = SampleDensity((column + 0.5f) * cellSize, (row + 0.5f) * cellSize);

                velocityX[index] += seconds * confinement * gradientY;
                velocityY[index] += seconds * ((-confinement * gradientX) - (Buoyancy * cellDensity));
            }
        }
    }

    private void ComputeDivergence()
    {
        float scale = 0.5f / cellSize;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                float right = velocityX[Index(column + 1, row)];
                float left = velocityX[Index(column - 1, row)];
                float below = velocityY[Index(column, row + 1)];
                float above = velocityY[Index(column, row - 1)];

                divergence[(row * columns) + column] = scale * ((right - left) + (below - above));
            }
        }
    }

    private void SolvePressure()
    {
        float cellArea = cellSize * cellSize;

        for (int index = 0; index < pressure.Length; index++)
        {
            pressure[index] *= PressureRetention;
        }

        for (int iteration = 0; iteration < PressureIterations; iteration++)
        {
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int index = (row * columns) + column;
                    float neighbors = GetPressure(column + 1, row) + GetPressure(column - 1, row) + GetPressure(column, row + 1) + GetPressure(column, row - 1);

                    nextPressure[index] = 0.25f * (neighbors - (cellArea * divergence[index]));
                }
            }

            (pressure, nextPressure) = (nextPressure, pressure);
        }
    }

    private void SubtractPressureGradient()
    {
        float scale = 0.5f / cellSize;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int index = (row * columns) + column;

                velocityX[index] -= scale * (GetPressure(column + 1, row) - GetPressure(column - 1, row));
                velocityY[index] -= scale * (GetPressure(column, row + 1) - GetPressure(column, row - 1));
            }
        }
    }

    private void AdvectVelocity(float seconds)
    {
        float decay = 1.0f / (1.0f + (VelocityDissipation * seconds));

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int index = (row * columns) + column;
                float x = ((column + 0.5f) * cellSize) - (seconds * velocityX[index]);
                float y = ((row + 0.5f) * cellSize) - (seconds * velocityY[index]);

                nextVelocityX[index] = decay * Sample(velocityX, columns, rows, cellSize, x, y);
                nextVelocityY[index] = decay * Sample(velocityY, columns, rows, cellSize, x, y);
            }
        }

        (velocityX, nextVelocityX) = (nextVelocityX, velocityX);
        (velocityY, nextVelocityY) = (nextVelocityY, velocityY);
    }

    private void AdvectDensity(float seconds)
    {
        float decay = 1.0f / (1.0f + (DensityDissipation * seconds));

        for (int row = 0; row < DensityRows; row++)
        {
            for (int column = 0; column < DensityColumns; column++)
            {
                float x = (column + 0.5f) * densityCellSize;
                float y = (row + 0.5f) * densityCellSize;
                float flowX = Sample(velocityX, columns, rows, cellSize, x, y);
                float flowY = Sample(velocityY, columns, rows, cellSize, x, y);

                nextDensity[(row * DensityColumns) + column] = decay * SampleDensity(x - (seconds * flowX), y - (seconds * flowY));
            }
        }

        (density, nextDensity) = (nextDensity, density);
    }

    private static (int First, int Last) GetRange(float center, float reach, float cell, int count)
    {
        int first = Math.Max((int)MathF.Floor((center - reach) / cell), 0);
        int last = Math.Min((int)MathF.Ceiling((center + reach) / cell), count - 1);

        return (first, last);
    }

    private int Index(int column, int row)
    {
        return (Math.Clamp(row, 0, rows - 1) * columns) + Math.Clamp(column, 0, columns - 1);
    }

    private float GetPressure(int column, int row)
    {
        if (column >= 0 && column < columns && row >= 0 && row < rows)
        {
            return pressure[(row * columns) + column];
        }
        else
        {
            return 0;
        }
    }

    private float SampleDensity(float x, float y)
    {
        return Sample(density, DensityColumns, DensityRows, densityCellSize, x, y);
    }

    private static float Sample(float[] field, int width, int height, float cell, float x, float y)
    {
        float gridX = Math.Clamp((x / cell) - 0.5f, 0, width - 1);
        float gridY = Math.Clamp((y / cell) - 0.5f, 0, height - 1);
        int left = (int)gridX;
        int top = (int)gridY;
        int right = Math.Min(left + 1, width - 1);
        int bottom = Math.Min(top + 1, height - 1);
        float weightX = gridX - left;
        float weightY = gridY - top;

        float upper = float.Lerp(field[(top * width) + left], field[(top * width) + right], weightX);
        float lower = float.Lerp(field[(bottom * width) + left], field[(bottom * width) + right], weightX);

        return float.Lerp(upper, lower, weightY);
    }
}
