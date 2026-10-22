using Avalonia;

namespace Gallean.Client;

internal sealed record AnimatedTextSmokeFrame(byte[] OpacityPixels, int Columns, int Rows, Rect Area, double Opacity, double NoiseSize);
