using Avalonia.Media;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class VideoGrainShaderPrecompileVisual : AbstractShaderPrecompileVisual
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
        VideoGrainShaderProgram program = new();
        SKRect area = SKRect.Create((float)Extent, (float)Extent);
        SKImageInfo info = new(PixelsSize, PixelsSize, SKColorType.Bgra8888, SKAlphaType.Premul);
        byte[] pixels = new byte[info.BytesSize];
        Array.Fill(pixels, byte.MaxValue);

        using SKImage image = SKImage.FromPixelCopy(info, pixels);
        using SKShader frame = image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, RenderSamplingOptions.Linear);
        using VideoGrainShaderProgram.Editor editor = program.Edit();

        editor.SetFrame(frame);

        using SKShader shader = editor.Build();
        using SKPaint paint = new() { Shader = shader };

        canvas.DrawRect(area, paint);
    }
}
