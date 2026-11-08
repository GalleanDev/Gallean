using Avalonia.Media;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class WorldMountainCloudShaderPrecompileVisual : AbstractShaderPrecompileVisual
{
    protected override Action<ImmediateDrawingContext> BuildPrecompile()
    {
        return Precompile;
    }

    private static void Precompile(ImmediateDrawingContext context)
    {
        using ISkiaSharpApiLease lease = context.GetSkiaApiLease();

        SKCanvas canvas = lease.SkCanvas;
        WorldMountainCloudShaderProgram program = WorldRenderContentProvider.MountainClouds.BuildProgram();
        SKRect area = SKRect.Create((float)Extent, (float)Extent);

        using SKShader mask = SKShader.CreateColor(SKColors.White);
        using WorldMountainCloudShaderProgram.Editor editor = program.Edit();

        editor.SetMask(mask);
        editor.SetTime(0);
        editor.SetPhase(0);
        editor.SetStrength(1);

        using SKShader shader = editor.Build();
        using SKPaint paint = new() { Shader = shader };

        canvas.DrawRect(area, paint);
    }
}
