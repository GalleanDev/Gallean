using Avalonia.Media;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class BattleWorldTransitionShaderPrecompileVisual : AbstractShaderPrecompileVisual
{
    protected override Action<ImmediateDrawingContext> BuildPrecompile()
    {
        return Precompile;
    }

    private static void Precompile(ImmediateDrawingContext context)
    {
        using ISkiaSharpApiLease lease = context.GetSkiaApiLease();

        SKCanvas canvas = lease.SkCanvas;
        SKRectI bounds = canvas.DeviceClipBounds;
        BattleWorldTransitionShaderProgram program = new();

        using ScaledSurface surface = ScaledSurface.Create(canvas, 1, SKMatrix.Identity);
        using SKShader image = surface.ToShader();
        using BattleWorldTransitionShaderProgram.Editor editor = program.Edit();

        editor.SetImage(image);

        using SKShader shader = editor.Build();
        using SKPaint paint = new() { Shader = shader };

        canvas.ResetMatrix();
        canvas.DrawRect(bounds, paint);
    }
}
