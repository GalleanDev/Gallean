using Avalonia.Media;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class BattleFluidShaderPrecompileVisual : AbstractShaderPrecompileVisual
{
    private const int PixelsSize = 2;

    protected override Action<ImmediateDrawingContext> BuildPrecompile()
    {
        return Precompile;
    }

    private static void Precompile(ImmediateDrawingContext context)
    {
        using ISkiaSharpApiLease lease = context.GetSkiaApiLease();

        SKCanvas canvas = lease.SkCanvas;
        BattleFluidShaderProgram program = new();
        SKRect area = SKRect.Create((float)Extent, (float)Extent);
        SKImageInfo info = new(PixelsSize, PixelsSize, SKColorType.Bgra8888, SKAlphaType.Premul);
        byte[] pixels = new byte[info.BytesSize];
        Array.Fill(pixels, byte.MaxValue);

        using SKImage image = SKImage.FromPixelCopy(info, pixels);
        using SKShader clamped = image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, RenderSamplingOptions.Linear);
        using BattleFluidShaderProgram.Editor editor = program.Edit();

        editor.SetThickness(clamped);
        editor.SetHeightField(clamped);

        using SKShader shader = editor.Build();
        using SKPaint paint = new() { Shader = shader };

        canvas.DrawRect(area, paint);
    }
}
