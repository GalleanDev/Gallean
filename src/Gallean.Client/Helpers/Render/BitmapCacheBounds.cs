using Avalonia;
using Avalonia.Rendering.Composition;

namespace Gallean.Client;

internal sealed class BitmapCacheBounds : AvaloniaObject
{
    public static readonly AttachedProperty<double?> OutsetProperty =
        AvaloniaProperty.RegisterAttached<BitmapCacheBounds, Visual, double?>("Outset");

    static BitmapCacheBounds()
    {
        OutsetProperty.Changed.AddClassHandler<Visual>(OnOutsetChanged);
    }

    public static void SetOutset(Visual element, double? value)
    {
        element.SetValue(OutsetProperty, value);
    }

    public static double? GetOutset(Visual element)
    {
        return element.GetValue(OutsetProperty);
    }

    private static (double Offset, double Size) SnapToPixels(double start, double end, double scale)
    {
        double firstPixel = Math.Floor(start * scale);
        double lastPixel = Math.Ceiling(end * scale);
        double offset = firstPixel / scale;
        double size = (lastPixel / scale) - offset;

        while ((int)((offset + size - offset) * scale) < lastPixel - firstPixel)
        {
            size = Math.BitIncrement(size);
        }

        return (offset, size);
    }

    private static void OnOutsetChanged(Visual visual, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is null)
        {
            visual.AttachedToVisualTree += OnAttachedToVisualTree;
            visual.PropertyChanged += OnPropertyChanged;
        }

        if (e.NewValue is null)
        {
            visual.AttachedToVisualTree -= OnAttachedToVisualTree;
            visual.PropertyChanged -= OnPropertyChanged;

            ElementComposition.SetElementChildVisual(visual, null);
        }
        else
        {
            UpdateAnchor(visual);
        }
    }

    private static void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        UpdateAnchor((Visual)sender!);
    }

    private static void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Visual.BoundsProperty || e.Property == UiScaleDecorator.RenderScalingProperty)
        {
            UpdateAnchor((Visual)sender!);
        }
    }

    private static void UpdateAnchor(Visual visual)
    {
        CompositionVisual? host = ElementComposition.GetElementVisual(visual);
        double? outset = GetOutset(visual);

        if (host is not null && outset is not null)
        {
            CompositionVisual? child = ElementComposition.GetElementChildVisual(visual);

            if (child is null || child.Compositor != host.Compositor)
            {
                child = host.Compositor.CreateSolidColorVisual();
                child.Opacity = 0;

                ElementComposition.SetElementChildVisual(visual, child);
            }

            double scale = UiScaleDecorator.GetRenderScaling(visual);
            Size size = visual.Bounds.Size;
            (double x, double width) = SnapToPixels(-outset.Value, size.Width + outset.Value, scale);
            (double y, double height) = SnapToPixels(-outset.Value, size.Height + outset.Value, scale);

            child.Offset = new Vector3D(x, y, 0);
            child.Size = new Vector(width, height);
        }
    }
}
