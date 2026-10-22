using Avalonia.Media;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class WorldHighlightShaderPrecompileVisual : AbstractShaderPrecompileVisual
{
    protected override Action<ImmediateDrawingContext> BuildPrecompile()
    {
        return Precompile;
    }

    private static void Precompile(ImmediateDrawingContext context)
    {
        using ISkiaSharpApiLease lease = context.GetSkiaApiLease();

        SKCanvas canvas = lease.SkCanvas;
        WorldHighlightShaderProgram program = new();
        SKRect area = SKRect.Create((float)Extent, (float)Extent);

        using SKPictureRecorder recorder = new();
        using SKPaint bodyPaint = SkiaHelpers.MakeImagePaint();

        SKCanvas bodyCanvas = recorder.BeginRecording(area);
        bodyCanvas.DrawRect(area, bodyPaint);

        using SKPicture picture = recorder.EndRecording();
        using SKShader body = picture.ToShader(SKShaderTileMode.Decal, SKShaderTileMode.Decal, SKFilterMode.Linear, SKMatrix.Identity, area);
        using WorldHighlightShaderProgram.Editor editor = program.Edit();

        editor.SetBody(body);

        using SKShader shader = editor.Build();
        using SKPaint paint = new() { Shader = shader };

        canvas.DrawRect(area, paint);
    }
}
