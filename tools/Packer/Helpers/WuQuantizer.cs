namespace Packer;

internal sealed partial class WuQuantizer(int maxColors)
{
    public readonly record struct RgbaColor(byte Red, byte Green, byte Blue, byte Alpha);

    private readonly record struct Sums(double Red, double Green, double Blue, double Alpha, double Weight);

    private const int IndexBits = 5;
    private const int IndexCount = (1 << IndexBits) + 1;
    private const int TransparentIndex = 1;
    private const int TranslucentIndex = 2;
    private const int TranslucentIndexCount = (1 << IndexBits) - 1;
    private const int TranslucentAlphaCount = byte.MaxValue - 1;
    private const int OpaqueIndex = TranslucentIndex + TranslucentIndexCount;
    private const int IndexAlphaCount = OpaqueIndex + 1;
    private const int TableLength = IndexCount * IndexCount * IndexCount * IndexAlphaCount;

    private readonly long[] weights = new long[TableLength];
    private readonly long[] momentsRed = new long[TableLength];
    private readonly long[] momentsGreen = new long[TableLength];
    private readonly long[] momentsBlue = new long[TableLength];
    private readonly long[] momentsAlpha = new long[TableLength];
    private readonly double[] moments = new double[TableLength];
    private readonly byte[] tags = new byte[TableLength];
    private readonly RgbaColor[] palette = new RgbaColor[maxColors];

    private int paletteLength = maxColors;
    private bool resolved;

    public void Reset()
    {
        Array.Clear(weights);
        Array.Clear(momentsRed);
        Array.Clear(momentsGreen);
        Array.Clear(momentsBlue);
        Array.Clear(momentsAlpha);
        Array.Clear(moments);
        Array.Clear(tags);
        Array.Clear(palette);

        paletteLength = palette.Length;
        resolved = false;
    }

    public void AddColors(ReadOnlySpan<byte> pixels)
    {
        for (int offset = 0; offset < pixels.Length; offset += 4)
        {
            int red = pixels[offset];
            int green = pixels[offset + 1];
            int blue = pixels[offset + 2];
            int alpha = pixels[offset + 3];

            int index = GetIndex(
                GetColorIndex(red),
                GetColorIndex(green),
                GetColorIndex(blue),
                GetAlphaIndex(alpha));

            weights[index]++;
            momentsRed[index] += red;
            momentsGreen[index] += green;
            momentsBlue[index] += blue;
            momentsAlpha[index] += alpha;
            moments[index] += (red * red) + (green * green) + (blue * blue) + (alpha * alpha);
        }
    }

    public void ResolvePalette()
    {
        if (resolved)
        {
            return;
        }

        resolved = true;

        BuildMoments();

        Box[] cubes = BuildCubes();

        for (int i = 0; i < paletteLength; i++)
        {
            Mark(cubes[i], (byte)i);

            double weight = Volume(cubes[i], weights);

            if (weight != 0)
            {
                palette[i] = new RgbaColor(
                    Round(Volume(cubes[i], momentsRed) / weight),
                    Round(Volume(cubes[i], momentsGreen) / weight),
                    Round(Volume(cubes[i], momentsBlue) / weight),
                    Round(Volume(cubes[i], momentsAlpha) / weight));
            }
            else
            {
                palette[i] = new RgbaColor(0, 0, 0, byte.MaxValue);
            }
        }
    }

    public RgbaColor GetQuantizedColor(byte red, byte green, byte blue, byte alpha)
    {
        int index = GetIndex(
            GetColorIndex(red),
            GetColorIndex(green),
            GetColorIndex(blue),
            GetAlphaIndex(alpha));

        return palette[tags[index]];
    }

    private static byte Round(double value)
    {
        return (byte)Math.Clamp(value + 0.5, byte.MinValue, byte.MaxValue);
    }

    private static int GetColorIndex(int color)
    {
        return (color >> (8 - IndexBits)) + 1;
    }

    private static int GetAlphaIndex(int alpha)
    {
        if (alpha == 0)
        {
            return TransparentIndex;
        }
        else if (alpha == byte.MaxValue)
        {
            return OpaqueIndex;
        }
        else
        {
            return TranslucentIndex + ((alpha - 1) * TranslucentIndexCount / TranslucentAlphaCount);
        }
    }

    private static int GetIndex(int red, int green, int blue, int alpha)
    {
        return (((((red * IndexCount) + green) * IndexCount) + blue) * IndexAlphaCount) + alpha;
    }

    private static double Volume(Box cube, long[] moment)
    {
        return moment[cube.Index1111]
             - moment[cube.Index1110]
             - moment[cube.Index1101]
             + moment[cube.Index1100]
             - moment[cube.Index1011]
             + moment[cube.Index1010]
             + moment[cube.Index1001]
             - moment[cube.Index1000]
             - moment[cube.Index0111]
             + moment[cube.Index0110]
             + moment[cube.Index0101]
             - moment[cube.Index0100]
             + moment[cube.Index0011]
             - moment[cube.Index0010]
             - moment[cube.Index0001]
             + moment[cube.Index0000];
    }

    private static long Bottom(Box cube, int direction, long[] moment)
    {
        return direction switch
        {
            3 => -moment[cube.Index0111]
               + moment[cube.Index0110]
               + moment[cube.Index0101]
               - moment[cube.Index0100]
               + moment[cube.Index0011]
               - moment[cube.Index0010]
               - moment[cube.Index0001]
               + moment[cube.Index0000],

            2 => -moment[cube.Index1011]
               + moment[cube.Index1010]
               + moment[cube.Index1001]
               - moment[cube.Index1000]
               + moment[cube.Index0011]
               - moment[cube.Index0010]
               - moment[cube.Index0001]
               + moment[cube.Index0000],

            1 => -moment[cube.Index1101]
               + moment[cube.Index1100]
               + moment[cube.Index1001]
               - moment[cube.Index1000]
               + moment[cube.Index0101]
               - moment[cube.Index0100]
               - moment[cube.Index0001]
               + moment[cube.Index0000],

            0 => -moment[cube.Index1110]
               + moment[cube.Index1100]
               + moment[cube.Index1010]
               - moment[cube.Index1000]
               + moment[cube.Index0110]
               - moment[cube.Index0100]
               - moment[cube.Index0010]
               + moment[cube.Index0000],

            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }

    private static long Top(Box cube, int direction, int position, long[] moment)
    {
        return direction switch
        {
            3 => moment[GetIndex(position, cube.GreenMax, cube.BlueMax, cube.AlphaMax)]
               - moment[GetIndex(position, cube.GreenMax, cube.BlueMax, cube.AlphaMin)]
               - moment[GetIndex(position, cube.GreenMax, cube.BlueMin, cube.AlphaMax)]
               + moment[GetIndex(position, cube.GreenMax, cube.BlueMin, cube.AlphaMin)]
               - moment[GetIndex(position, cube.GreenMin, cube.BlueMax, cube.AlphaMax)]
               + moment[GetIndex(position, cube.GreenMin, cube.BlueMax, cube.AlphaMin)]
               + moment[GetIndex(position, cube.GreenMin, cube.BlueMin, cube.AlphaMax)]
               - moment[GetIndex(position, cube.GreenMin, cube.BlueMin, cube.AlphaMin)],

            2 => moment[GetIndex(cube.RedMax, position, cube.BlueMax, cube.AlphaMax)]
               - moment[GetIndex(cube.RedMax, position, cube.BlueMax, cube.AlphaMin)]
               - moment[GetIndex(cube.RedMax, position, cube.BlueMin, cube.AlphaMax)]
               + moment[GetIndex(cube.RedMax, position, cube.BlueMin, cube.AlphaMin)]
               - moment[GetIndex(cube.RedMin, position, cube.BlueMax, cube.AlphaMax)]
               + moment[GetIndex(cube.RedMin, position, cube.BlueMax, cube.AlphaMin)]
               + moment[GetIndex(cube.RedMin, position, cube.BlueMin, cube.AlphaMax)]
               - moment[GetIndex(cube.RedMin, position, cube.BlueMin, cube.AlphaMin)],

            1 => moment[GetIndex(cube.RedMax, cube.GreenMax, position, cube.AlphaMax)]
               - moment[GetIndex(cube.RedMax, cube.GreenMax, position, cube.AlphaMin)]
               - moment[GetIndex(cube.RedMax, cube.GreenMin, position, cube.AlphaMax)]
               + moment[GetIndex(cube.RedMax, cube.GreenMin, position, cube.AlphaMin)]
               - moment[GetIndex(cube.RedMin, cube.GreenMax, position, cube.AlphaMax)]
               + moment[GetIndex(cube.RedMin, cube.GreenMax, position, cube.AlphaMin)]
               + moment[GetIndex(cube.RedMin, cube.GreenMin, position, cube.AlphaMax)]
               - moment[GetIndex(cube.RedMin, cube.GreenMin, position, cube.AlphaMin)],

            0 => moment[GetIndex(cube.RedMax, cube.GreenMax, cube.BlueMax, position)]
               - moment[GetIndex(cube.RedMax, cube.GreenMax, cube.BlueMin, position)]
               - moment[GetIndex(cube.RedMax, cube.GreenMin, cube.BlueMax, position)]
               + moment[GetIndex(cube.RedMax, cube.GreenMin, cube.BlueMin, position)]
               - moment[GetIndex(cube.RedMin, cube.GreenMax, cube.BlueMax, position)]
               + moment[GetIndex(cube.RedMin, cube.GreenMax, cube.BlueMin, position)]
               + moment[GetIndex(cube.RedMin, cube.GreenMin, cube.BlueMax, position)]
               - moment[GetIndex(cube.RedMin, cube.GreenMin, cube.BlueMin, position)],

            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };
    }

    private void BuildMoments()
    {
        long[] volume = new long[IndexCount * IndexAlphaCount];
        long[] volumeRed = new long[IndexCount * IndexAlphaCount];
        long[] volumeGreen = new long[IndexCount * IndexAlphaCount];
        long[] volumeBlue = new long[IndexCount * IndexAlphaCount];
        long[] volumeAlpha = new long[IndexCount * IndexAlphaCount];
        double[] volumeMoment = new double[IndexCount * IndexAlphaCount];

        long[] area = new long[IndexAlphaCount];
        long[] areaRed = new long[IndexAlphaCount];
        long[] areaGreen = new long[IndexAlphaCount];
        long[] areaBlue = new long[IndexAlphaCount];
        long[] areaAlpha = new long[IndexAlphaCount];
        double[] areaMoment = new double[IndexAlphaCount];

        int baseIndex = GetIndex(1, 0, 0, 0);

        for (int red = 1; red < IndexCount; red++)
        {
            Array.Clear(volume);
            Array.Clear(volumeRed);
            Array.Clear(volumeGreen);
            Array.Clear(volumeBlue);
            Array.Clear(volumeAlpha);
            Array.Clear(volumeMoment);

            for (int green = 1; green < IndexCount; green++)
            {
                Array.Clear(area);
                Array.Clear(areaRed);
                Array.Clear(areaGreen);
                Array.Clear(areaBlue);
                Array.Clear(areaAlpha);
                Array.Clear(areaMoment);

                for (int blue = 1; blue < IndexCount; blue++)
                {
                    long line = 0;
                    long lineRed = 0;
                    long lineGreen = 0;
                    long lineBlue = 0;
                    long lineAlpha = 0;
                    double lineMoment = 0;

                    for (int alpha = 1; alpha < IndexAlphaCount; alpha++)
                    {
                        int index = GetIndex(red, green, blue, alpha);

                        line += weights[index];
                        lineRed += momentsRed[index];
                        lineGreen += momentsGreen[index];
                        lineBlue += momentsBlue[index];
                        lineAlpha += momentsAlpha[index];
                        lineMoment += moments[index];

                        area[alpha] += line;
                        areaRed[alpha] += lineRed;
                        areaGreen[alpha] += lineGreen;
                        areaBlue[alpha] += lineBlue;
                        areaAlpha[alpha] += lineAlpha;
                        areaMoment[alpha] += lineMoment;

                        int volumeIndex = (blue * IndexAlphaCount) + alpha;

                        volume[volumeIndex] += area[alpha];
                        volumeRed[volumeIndex] += areaRed[alpha];
                        volumeGreen[volumeIndex] += areaGreen[alpha];
                        volumeBlue[volumeIndex] += areaBlue[alpha];
                        volumeAlpha[volumeIndex] += areaAlpha[alpha];
                        volumeMoment[volumeIndex] += areaMoment[alpha];

                        int previous = index - baseIndex;

                        weights[index] = weights[previous] + volume[volumeIndex];
                        momentsRed[index] = momentsRed[previous] + volumeRed[volumeIndex];
                        momentsGreen[index] = momentsGreen[previous] + volumeGreen[volumeIndex];
                        momentsBlue[index] = momentsBlue[previous] + volumeBlue[volumeIndex];
                        momentsAlpha[index] = momentsAlpha[previous] + volumeAlpha[volumeIndex];
                        moments[index] = moments[previous] + volumeMoment[volumeIndex];
                    }
                }
            }
        }
    }

    private double Variance(Box cube)
    {
        double red = Volume(cube, momentsRed);
        double green = Volume(cube, momentsGreen);
        double blue = Volume(cube, momentsBlue);
        double alpha = Volume(cube, momentsAlpha);

        double moment =
            moments[cube.Index1111]
          - moments[cube.Index1110]
          - moments[cube.Index1101]
          + moments[cube.Index1100]
          - moments[cube.Index1011]
          + moments[cube.Index1010]
          + moments[cube.Index1001]
          - moments[cube.Index1000]
          - moments[cube.Index0111]
          + moments[cube.Index0110]
          + moments[cube.Index0101]
          - moments[cube.Index0100]
          + moments[cube.Index0011]
          - moments[cube.Index0010]
          - moments[cube.Index0001]
          + moments[cube.Index0000];

        return moment - (((red * red) + (green * green) + (blue * blue) + (alpha * alpha)) / Volume(cube, weights));
    }

    private (double Max, int Cut) Maximize(Box cube, int direction, int first, int last, Sums whole)
    {
        long baseRed = Bottom(cube, direction, momentsRed);
        long baseGreen = Bottom(cube, direction, momentsGreen);
        long baseBlue = Bottom(cube, direction, momentsBlue);
        long baseAlpha = Bottom(cube, direction, momentsAlpha);
        long baseWeight = Bottom(cube, direction, weights);

        double max = 0;
        int cut = -1;

        for (int i = first; i < last; i++)
        {
            double halfWeight = baseWeight + Top(cube, direction, i, weights);

            if (halfWeight == 0)
            {
                continue;
            }

            double otherWeight = whole.Weight - halfWeight;

            if (otherWeight == 0)
            {
                continue;
            }

            double halfRed = baseRed + Top(cube, direction, i, momentsRed);
            double halfGreen = baseGreen + Top(cube, direction, i, momentsGreen);
            double halfBlue = baseBlue + Top(cube, direction, i, momentsBlue);
            double halfAlpha = baseAlpha + Top(cube, direction, i, momentsAlpha);

            double temp = ((halfRed * halfRed) + (halfGreen * halfGreen) + (halfBlue * halfBlue) + (halfAlpha * halfAlpha)) / halfWeight;

            double otherRed = whole.Red - halfRed;
            double otherGreen = whole.Green - halfGreen;
            double otherBlue = whole.Blue - halfBlue;
            double otherAlpha = whole.Alpha - halfAlpha;

            temp += ((otherRed * otherRed) + (otherGreen * otherGreen) + (otherBlue * otherBlue) + (otherAlpha * otherAlpha)) / otherWeight;

            if (temp > max)
            {
                max = temp;
                cut = i;
            }
        }

        return (max, cut);
    }

    private bool Cut(Box set1, Box set2)
    {
        Sums whole = new(
            Volume(set1, momentsRed),
            Volume(set1, momentsGreen),
            Volume(set1, momentsBlue),
            Volume(set1, momentsAlpha),
            Volume(set1, weights));

        (double maxRed, int cutRed) = Maximize(set1, 3, set1.RedMin + 1, set1.RedMax, whole);
        (double maxGreen, int cutGreen) = Maximize(set1, 2, set1.GreenMin + 1, set1.GreenMax, whole);
        (double maxBlue, int cutBlue) = Maximize(set1, 1, set1.BlueMin + 1, set1.BlueMax, whole);
        (double maxAlpha, int cutAlpha) = Maximize(set1, 0, set1.AlphaMin + 1, set1.AlphaMax, whole);

        int direction;

        if (maxRed >= maxGreen && maxRed >= maxBlue && maxRed >= maxAlpha)
        {
            direction = 3;

            if (cutRed < 0)
            {
                return false;
            }
        }
        else if (maxGreen >= maxRed && maxGreen >= maxBlue && maxGreen >= maxAlpha)
        {
            direction = 2;
        }
        else if (maxBlue >= maxRed && maxBlue >= maxGreen && maxBlue >= maxAlpha)
        {
            direction = 1;
        }
        else
        {
            direction = 0;
        }

        set2.RedMax = set1.RedMax;
        set2.GreenMax = set1.GreenMax;
        set2.BlueMax = set1.BlueMax;
        set2.AlphaMax = set1.AlphaMax;

        switch (direction)
        {
            case 3:
                set2.RedMin = set1.RedMax = cutRed;
                set2.GreenMin = set1.GreenMin;
                set2.BlueMin = set1.BlueMin;
                set2.AlphaMin = set1.AlphaMin;
                break;

            case 2:
                set2.GreenMin = set1.GreenMax = cutGreen;
                set2.RedMin = set1.RedMin;
                set2.BlueMin = set1.BlueMin;
                set2.AlphaMin = set1.AlphaMin;
                break;

            case 1:
                set2.BlueMin = set1.BlueMax = cutBlue;
                set2.RedMin = set1.RedMin;
                set2.GreenMin = set1.GreenMin;
                set2.AlphaMin = set1.AlphaMin;
                break;

            case 0:
                set2.AlphaMin = set1.AlphaMax = cutAlpha;
                set2.RedMin = set1.RedMin;
                set2.GreenMin = set1.GreenMin;
                set2.BlueMin = set1.BlueMin;
                break;
        }

        set1.Update();
        set2.Update();

        return true;
    }

    private void Mark(Box cube, byte label)
    {
        for (int red = cube.RedMin + 1; red <= cube.RedMax; red++)
        {
            for (int green = cube.GreenMin + 1; green <= cube.GreenMax; green++)
            {
                for (int blue = cube.BlueMin + 1; blue <= cube.BlueMax; blue++)
                {
                    for (int alpha = cube.AlphaMin + 1; alpha <= cube.AlphaMax; alpha++)
                    {
                        tags[GetIndex(red, green, blue, alpha)] = label;
                    }
                }
            }
        }
    }

    private Box[] BuildCubes()
    {
        Box[] alphaClasses =
        [
            CreateCube(TransparentIndex, TransparentIndex),
            CreateCube(TranslucentIndex, OpaqueIndex - 1),
            CreateCube(OpaqueIndex, OpaqueIndex)
        ];

        Box[] cubes = new Box[palette.Length];
        double[] variances = new double[palette.Length];
        int cubeCount = 0;

        foreach (Box alphaClass in alphaClasses)
        {
            if (Volume(alphaClass, weights) > 0)
            {
                cubes[cubeCount] = alphaClass;
                variances[cubeCount] = Variance(alphaClass);
                cubeCount++;
            }
        }

        for (int i = cubeCount; i < cubes.Length; i++)
        {
            cubes[i] = new Box();
        }

        int next = FindNextCube(variances, cubeCount);

        for (int i = cubeCount; i < cubes.Length; i++)
        {
            if (Cut(cubes[next], cubes[i]))
            {
                variances[next] = cubes[next].Volume > 1 ? Variance(cubes[next]) : 0;
                variances[i] = cubes[i].Volume > 1 ? Variance(cubes[i]) : 0;
            }
            else
            {
                variances[next] = 0;
                i--;
            }

            next = FindNextCube(variances, i + 1);

            if (variances[next] <= 0)
            {
                paletteLength = i + 1;
                break;
            }
        }

        return cubes;
    }

    private static Box CreateCube(int firstAlphaIndex, int lastAlphaIndex)
    {
        Box cube = new()
        {
            RedMax = IndexCount - 1,
            GreenMax = IndexCount - 1,
            BlueMax = IndexCount - 1,
            AlphaMin = firstAlphaIndex - 1,
            AlphaMax = lastAlphaIndex
        };

        cube.Update();

        return cube;
    }

    private static int FindNextCube(double[] variances, int count)
    {
        int largest = 0;

        for (int i = 1; i < count; i++)
        {
            if (variances[i] > variances[largest])
            {
                largest = i;
            }
        }

        return largest;
    }
}
