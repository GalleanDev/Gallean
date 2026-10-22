using Avalonia;
using Avalonia.Media;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class DiagnosticsVisual : AbstractAnimatedVisual
{
    private readonly Lazy<DiagnosticsPanel> panel = new();

    public DiagnosticsVisual()
    {
        Focusable = false;
        IsHitTestVisible = false;
    }

    private DevToolsViewModel ViewModel => (DevToolsViewModel)DataContext!;

    protected override Size MeasureOverride(Size availableSize)
    {
        SKSize size = panel.Value.Size;

        return new Size(size.Width, size.Height);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        context.Custom(new DiagnosticsRenderOperation(new Rect(Bounds.Size), panel.Value, ViewModel.Diagnostics));
    }

    protected override void OnFrame(TimeSpan frameTime)
    {
        if (IsEffectivelyVisible)
        {
            InvalidateVisual();
        }
    }
}
