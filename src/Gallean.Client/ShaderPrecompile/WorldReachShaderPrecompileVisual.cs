using Avalonia.Media;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class WorldReachShaderPrecompileVisual : AbstractShaderPrecompileVisual
{
    protected override Action<ImmediateDrawingContext> BuildPrecompile()
    {
        return Precompile;
    }

    private static void Precompile(ImmediateDrawingContext context)
    {
        using ISkiaSharpApiLease lease = context.GetSkiaApiLease();

        SKCanvas canvas = lease.SkCanvas;
        WorldReachShaderProgram program = new();
        SKImageInfo info = new(1, 1, SKColorType.Alpha8, SKAlphaType.Premul);

        using SKImage image = SKImage.FromPixelCopy(info, [byte.MaxValue]);
        using SKShader tiles = image.ToShader(SKShaderTileMode.Decal, SKShaderTileMode.Decal, RenderSamplingOptions.Nearest);
        using SKVertices mesh = SKVertices.CreateCopy(SKVertexMode.Triangles, [new SKPoint(0, 0), new SKPoint((float)Extent, 0), new SKPoint(0, (float)Extent)], null, null);
        using WorldReachShaderProgram.Editor editor = program.Edit();

        editor.SetTiles(tiles);

        using SKShader shader = editor.Build();
        using SKPaint paint = SkiaHelpers.MakeImagePaint();
        paint.Shader = shader;

        canvas.DrawVertices(mesh, SKBlendMode.Modulate, paint);
    }
}
