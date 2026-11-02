using Avalonia.Media;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class BattleUnitShaderPrecompileVisual : AbstractShaderPrecompileVisual
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
        BattleUnitShaderProgram program = new();
        SKRect area = SKRect.Create((float)Extent, (float)Extent);
        SKImageInfo spriteInfo = new(PixelsSize, PixelsSize, SKColorType.Bgra8888, SKAlphaType.Premul);
        SKImageInfo mapInfo = new(PixelsSize, PixelsSize, SKColorType.Rgba8888, SKAlphaType.Premul);
        byte[] pixels = new byte[spriteInfo.BytesSize];
        Array.Fill(pixels, byte.MaxValue);

        using SKImage sprite = SKImage.FromPixelCopy(spriteInfo, pixels);
        using SKImage map = SKImage.FromPixelCopy(mapInfo, pixels);
        using SKShader decal = sprite.ToShader(SKShaderTileMode.Decal, SKShaderTileMode.Decal, RenderSamplingOptions.Linear);
        using SKShader clamped = map.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, RenderSamplingOptions.Linear);
        using BattleUnitShaderProgram.Editor editor = program.Edit();

        editor.SetSource(decal);
        editor.SetNormals(decal);
        editor.SetAmbient(clamped);
        editor.SetReflection(clamped);

        using SKShader shader = editor.Build();
        using SKPaint paint = SkiaHelpers.MakeImagePaint();
        paint.Shader = shader;

        canvas.DrawRect(area, paint);
    }
}
