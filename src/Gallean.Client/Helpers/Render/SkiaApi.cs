using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace Gallean.Client;

internal static unsafe partial class SkiaApi
{
    public static void DrawAtlas(
        SKCanvas canvas,
        SKImage atlas,
        ReadOnlySpan<SKRotationScaleMatrix> transforms,
        ReadOnlySpan<SKRect> sources,
        SKBlendMode blendMode,
        SKSamplingOptions sampling,
        SKRect cullRect,
        SKPaint paint)
    {
        fixed (SKRotationScaleMatrix* xform = transforms)
        fixed (SKRect* tex = sources)
        {
            sk_canvas_draw_atlas(
                canvas.Handle,
                atlas.Handle,
                xform,
                tex,
                null,
                sources.Length,
                blendMode,
                &sampling,
                &cullRect,
                paint.Handle);
        }

        GC.KeepAlive(canvas);
        GC.KeepAlive(atlas);
        GC.KeepAlive(paint);
    }

    [LibraryImport("libSkiaSharp")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void sk_canvas_draw_atlas(
        IntPtr ccanvas,
        IntPtr atlas,
        SKRotationScaleMatrix* xform,
        SKRect* tex,
        uint* colors,
        int count,
        SKBlendMode mode,
        SKSamplingOptions* sampling,
        SKRect* cullRect,
        IntPtr paint);
}
