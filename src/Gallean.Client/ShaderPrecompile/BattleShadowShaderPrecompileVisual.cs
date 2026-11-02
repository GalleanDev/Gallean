using Avalonia.Media;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class BattleShadowShaderPrecompileVisual : AbstractShaderPrecompileVisual
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
        BattleShadowShaderProgram program = new();
        SKRect area = SKRect.Create((float)Extent, (float)Extent);
        SKImageInfo info = new(PixelsSize, PixelsSize, SKColorType.Bgra8888, SKAlphaType.Premul);
        SKMatrix perspective = new() { ScaleX = 1, ScaleY = 1, Persp1 = (float)(1.0 / Extent), Persp2 = 1 };
        byte[] pixels = new byte[info.BytesSize];
        Array.Fill(pixels, byte.MaxValue);

        using SKImage image = SKImage.FromPixelCopy(info, pixels);
        using SKShader decal = image.ToShader(SKShaderTileMode.Decal, SKShaderTileMode.Decal, RenderSamplingOptions.Linear);
        using BattleShadowShaderProgram.Editor editor = program.Edit();

        editor.SetSource(decal);

        using SKShader shader = editor.Build();
        using SKPaint paint = new() { Shader = shader, BlendMode = SKBlendMode.Modulate };

        canvas.Save();
        canvas.Concat(perspective);
        canvas.DrawRect(area, paint);
        canvas.Restore();
    }
}
