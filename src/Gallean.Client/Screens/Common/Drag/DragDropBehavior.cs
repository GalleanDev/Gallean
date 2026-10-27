using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class DragDropBehavior(Control view)
{
    private const double DragThreshold = 5;
    private const string DragImageClass = "drag-image";
    private const string DropZoneClass = "drop-zone";
    private const string DragOverClass = "drag-over";

    private static readonly Size SmallUnitImageSize = new(76, 91);
    private static readonly Size LargeUnitImageSize = new(156, 91);
    private static readonly Size ItemImageSize = new(76, 76);

    private DragDropViewModel? dragDrop;
    private CursorDragImage? dragImage;
    private TopLevel? dragImageTopLevel;
    private Control? hoverZone;
    private Visual? pressElement;
    private IDragSource? pressSource;
    private Point pressPosition;
    private double pressScale = 1;

    public static void Attach(Control view)
    {
        DragDropBehavior behavior = new(view);

        view.AddHandler(InputElement.PointerPressedEvent, behavior.OnDragPointerPressed, RoutingStrategies.Tunnel, true);
        view.AddHandler(InputElement.PointerMovedEvent, behavior.OnDragPointerMoved, RoutingStrategies.Bubble, true);
        view.AddHandler(InputElement.PointerReleasedEvent, behavior.OnDragPointerReleased, RoutingStrategies.Bubble, true);
        view.AddHandler(InputElement.PointerCaptureLostEvent, behavior.OnDragPointerCaptureLost, RoutingStrategies.Bubble, true);
        view.DetachedFromVisualTree += behavior.OnViewDetachedFromVisualTree;
    }

    private static Size GetImageSize(DragPreviewType type)
    {
        return type switch
        {
            DragPreviewType.SmallUnit => SmallUnitImageSize,
            DragPreviewType.LargeUnit => LargeUnitImageSize,
            DragPreviewType.Item => ItemImageSize
        };
    }

    private static IDragSourceReference GetDragReference(IDragSource source, KeyModifiers modifiers)
    {
        if (modifiers.HasFlag(KeyModifiers.Shift) && source is IGroupDragSource groupSource)
        {
            return groupSource.GroupDragReference;
        }
        else
        {
            return source.DragReference;
        }
    }

    private static CursorDragImage RenderDragImage(DragPreview preview, Visual source, double scale, double renderScaling)
    {
        double pixelScale = scale * renderScaling;
        Size size = GetImageSize(preview.Type);
        PixelSize pixelSize = PixelSize.FromSize(size, pixelScale);
        Vector dpi = new(96 * pixelScale, 96 * pixelScale);

        ContentControl adorner = new()
        {
            Content = preview,
            Width = size.Width,
            Height = size.Height
        };

        adorner.Classes.Add(DragImageClass);
        AdornerLayer.SetAdorner(source, adorner);
        adorner.Measure(size);
        adorner.Arrange(new Rect(size));

        using RenderTargetBitmap target = new(pixelSize, dpi);
        target.Render(adorner);

        AdornerLayer.SetAdorner(source, null);

        SKImageInfo info = new(pixelSize.Width, pixelSize.Height, SKColorType.Bgra8888, SKAlphaType.Premul);

        using SKBitmap pixels = new(info);
        target.CopyPixels(new PixelRect(pixelSize), pixels.GetPixels(), pixels.ByteCount, pixels.RowBytes);

        SKImage image = SKImage.FromBitmap(pixels);

        return new CursorDragImage(image, size * scale);
    }

    private Visual? FindElementAt<T>(Point point) where T : class
    {
        foreach (Visual visual in view.GetVisualsAt(point))
        {
            for (Visual? current = visual; current is not null; current = current.GetVisualParent())
            {
                if (current.DataContext is T)
                {
                    return current;
                }
            }
        }

        return null;
    }

    private double ScaleAt(Point point)
    {
        foreach (Visual visual in view.GetVisualsAt(point))
        {
            Matrix? transform = visual.TransformToVisual(view);

            if (transform is not null)
            {
                return transform.Value.M11;
            }
        }

        return 1;
    }

    private Control? FindDropZoneAt(Point point)
    {
        foreach (Visual visual in view.GetVisualsAt(point))
        {
            for (Visual? current = visual; current is not null; current = current.GetVisualParent())
            {
                if (current is Control control && control.Classes.Contains(DropZoneClass))
                {
                    return control;
                }
            }
        }

        return null;
    }

    private void OnDragPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        pressElement = null;
        pressSource = null;

        PointerPoint point = e.GetCurrentPoint(view);

        if (dragDrop is null && point.Properties.IsLeftButtonPressed)
        {
            Point position = e.GetPosition(view);
            Visual? element = FindElementAt<IDragSource>(position);
            IDragSource? source = element?.DataContext as IDragSource;

            if (source?.DragPreview is not null)
            {
                pressElement = element;
                pressSource = source;
                pressPosition = position;
                pressScale = ScaleAt(position);
            }
        }
    }

    private void OnDragPointerMoved(object? sender, PointerEventArgs e)
    {
        Point point = e.GetPosition(view);
        Vector delta = point - pressPosition;

        if (dragDrop is not null)
        {
            UpdateHoverZone(point);
            e.Handled = true;
        }
        else if (pressElement is not null && pressSource is not null && delta.Length >= DragThreshold)
        {
            Visual element = pressElement;
            IDragSource source = pressSource;

            pressElement = null;
            pressSource = null;

            if (TryBeginDrag(element, source, e.KeyModifiers))
            {
                e.Pointer.Capture(view);
                UpdateHoverZone(point);
                e.Handled = true;
            }
        }
    }

    private void OnDragPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        pressElement = null;
        pressSource = null;

        if (dragDrop is not null)
        {
            DragDropViewModel current = dragDrop;
            Visual? targetElement = FindElementAt<IDropTarget>(e.GetPosition(view));
            IDropTarget? target = targetElement?.DataContext as IDropTarget;

            EndDragImage();

            e.Pointer.Capture(null);
            e.Handled = true;

            if (target is not null)
            {
                current.DropAsync(target.DropReference).FileAndForget();
            }
            else
            {
                current.EndDrag();
            }
        }
    }

    private void OnDragPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (dragDrop is not null)
        {
            if (ReferenceEquals(e.Pointer.Captured, view))
            {
                return;
            }

            DragDropViewModel current = dragDrop;

            EndDragImage();
            current.EndDrag();
        }
    }

    private void OnViewDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (dragDrop is not null)
        {
            DragDropViewModel current = dragDrop;

            EndDragImage();
            current.EndDrag();
        }
    }

    private bool TryBeginDrag(Visual element, IDragSource source, KeyModifiers modifiers)
    {
        DragPreview? preview = source.DragPreview;

        if (preview is not null && view.DataContext is IDragDropHost host)
        {
            DragDropViewModel current = host.DragDrop;

            IDragSourceReference reference = GetDragReference(source, modifiers);

            if (current.TryBeginDrag(reference))
            {
                dragDrop = current;
                ShowDragImage(preview, element);

                return true;
            }
        }

        return false;
    }

    private void ShowDragImage(DragPreview preview, Visual source)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(view);

        if (topLevel is not null)
        {
            dragImage = RenderDragImage(preview, source, pressScale, UiScaleDecorator.GetRenderScaling(view, topLevel));
            dragImageTopLevel = topLevel;
            CursorScope.SetDragImage(topLevel, dragImage);
        }
    }

    private void UpdateHoverZone(Point point)
    {
        Control? zone = FindDropZoneAt(point);

        if (ReferenceEquals(zone, hoverZone))
        {
            return;
        }

        hoverZone?.Classes.Remove(DragOverClass);
        zone?.Classes.Add(DragOverClass);
        hoverZone = zone;
    }

    private void EndDragImage()
    {
        hoverZone?.Classes.Remove(DragOverClass);
        hoverZone = null;
        dragDrop = null;

        if (dragImageTopLevel is not null)
        {
            CursorScope.SetDragImage(dragImageTopLevel, null);
            dragImageTopLevel = null;
        }

        dragImage?.Image.Dispose();
        dragImage = null;
    }
}
