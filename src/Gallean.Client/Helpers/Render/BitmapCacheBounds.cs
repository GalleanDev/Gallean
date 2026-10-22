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
        if (e.Property == Visual.BoundsProperty)
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

            Rect bounds = new Rect(visual.Bounds.Size).Inflate(outset.Value);

            child.Offset = new Vector3D(bounds.X, bounds.Y, 0);
            child.Size = new Vector(bounds.Width, bounds.Height);
        }
    }
}
