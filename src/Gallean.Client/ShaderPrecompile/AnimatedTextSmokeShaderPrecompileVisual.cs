using Avalonia;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed class AnimatedTextSmokeShaderPrecompileVisual : AbstractShaderPrecompileVisual
{
    private const int PixelsSize = 2;
    private const double NoiseSize = 12.0;

    protected override Action<ImmediateDrawingContext> BuildPrecompile()
    {
        byte[] pixels = new byte[PixelsSize * PixelsSize];
        Array.Fill(pixels, byte.MaxValue);

        AnimatedTextSmokeFrame smoke = new(
            pixels,
            PixelsSize,
            PixelsSize,
            new Rect(0, 0, Extent, Extent),
            Opacity: 1.0,
            NoiseSize);

        AnimatedTextRenderOperation operation = new(
            [],
            smoke,
            Client.Theme.Briefing.EmberColor,
            Client.Theme.Briefing.SmokeColor,
            Colors.Transparent,
            outlineThickness: 0.0,
            glowBlur: 0.0,
            TimeSpan.Zero);

        return operation.Render;
    }
}
