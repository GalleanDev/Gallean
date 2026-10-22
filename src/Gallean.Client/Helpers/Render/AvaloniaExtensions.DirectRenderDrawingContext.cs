using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;

namespace Gallean.Client;

internal static partial class AvaloniaExtensions
{
    private sealed class DirectRenderDrawingContext(IDrawingContextImplWithEffects inner) : IDrawingContextImplWithEffects
    {
        public Matrix Transform
        {
            get => inner.Transform;
            set => inner.Transform = value;
        }

        public void Clear(Color color)
        {
        }

        public void DrawBitmap(IBitmapImpl source, double opacity, Rect sourceRect, Rect destRect) => inner.DrawBitmap(source, opacity, sourceRect, destRect);
        public void DrawBitmap(IBitmapImpl source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect) => inner.DrawBitmap(source, opacityMask, opacityMaskRect, destRect);
        public void DrawLine(IPen? pen, Point p1, Point p2) => inner.DrawLine(pen, p1, p2);
        public void DrawGeometry(IBrush? brush, IPen? pen, IGeometryImpl geometry) => inner.DrawGeometry(brush, pen, geometry);
        public void DrawRectangle(IBrush? brush, IPen? pen, RoundedRect rect, BoxShadows boxShadows = default) => inner.DrawRectangle(brush, pen, rect, boxShadows);
        public void DrawRegion(IBrush? brush, IPen? pen, IPlatformRenderInterfaceRegion region) => inner.DrawRegion(brush, pen, region);
        public void DrawEllipse(IBrush? brush, IPen? pen, Rect rect) => inner.DrawEllipse(brush, pen, rect);
        public void DrawGlyphRun(IBrush? foreground, IGlyphRunImpl glyphRun) => inner.DrawGlyphRun(foreground, glyphRun);
        public IDrawingContextLayerImpl CreateLayer(PixelSize size) => inner.CreateLayer(size);
        public void PushClip(Rect clip) => inner.PushClip(clip);
        public void PushClip(RoundedRect clip) => inner.PushClip(clip);
        public void PushClip(IPlatformRenderInterfaceRegion region) => inner.PushClip(region);
        public void PopClip() => inner.PopClip();
        public void PushLayer(Rect bounds) => inner.PushLayer(bounds);
        public void PopLayer() => inner.PopLayer();
        public void PushOpacity(double opacity, Rect? bounds) => inner.PushOpacity(opacity, bounds);
        public void PopOpacity() => inner.PopOpacity();
        public void PushOpacityMask(IBrush mask, Rect bounds) => inner.PushOpacityMask(mask, bounds);
        public void PopOpacityMask() => inner.PopOpacityMask();
        public void PushGeometryClip(IGeometryImpl clip) => inner.PushGeometryClip(clip);
        public void PopGeometryClip() => inner.PopGeometryClip();
        public void PushRenderOptions(RenderOptions renderOptions) => inner.PushRenderOptions(renderOptions);
        public void PopRenderOptions() => inner.PopRenderOptions();
        public void PushTextOptions(TextOptions textOptions) => inner.PushTextOptions(textOptions);
        public void PopTextOptions() => inner.PopTextOptions();
        public void PushEffect(Rect? clipRect, IEffect effect) => inner.PushEffect(clipRect, effect);
        public void PopEffect() => inner.PopEffect();
        public object? GetFeature(Type t) => inner.GetFeature(t);
        public void Dispose() => inner.Dispose();
    }
}
