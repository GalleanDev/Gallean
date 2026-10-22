using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed class CoverBoxDecorator : Decorator
{
    public static readonly StyledProperty<Thickness> CropMarginProperty =
        AvaloniaProperty.Register<CoverBoxDecorator, Thickness>(nameof(CropMargin));

    static CoverBoxDecorator()
    {
        AffectsArrange<CoverBoxDecorator>(CropMarginProperty);
    }

    public CoverBoxDecorator()
    {
        ClipToBounds = true;
    }

    public Thickness CropMargin
    {
        get => GetValue(CropMarginProperty);
        set => SetValue(CropMarginProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Child?.Measure(Size.Infinity);

        Size content = Child?.DesiredSize ?? default;

        double width = double.IsInfinity(availableSize.Width) ? content.Width : availableSize.Width;
        double height = double.IsInfinity(availableSize.Height) ? content.Height : availableSize.Height;

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Control? child = Child;

        if (child is null)
        {
            return finalSize;
        }

        Size content = child.DesiredSize;

        if (content is { Width: > 0, Height: > 0 })
        {
            child.Arrange(new Rect(content));

            Rect kept = CalculateKeptArea(content);
            double scale = CalculateScale(finalSize, content, kept);

            double anchorX = GetAnchor(child.HorizontalAlignment);
            double anchorY = GetAnchor(child.VerticalAlignment);

            double offsetX = CalculateOffset(finalSize.Width, content.Width * scale, anchorX, kept.X * scale, kept.Width * scale);
            double offsetY = CalculateOffset(finalSize.Height, content.Height * scale, anchorY, kept.Y * scale, kept.Height * scale);

            child.RenderTransformOrigin = RelativePoint.TopLeft;
            child.RenderTransform = new MatrixTransform(new Matrix(scale, 0, 0, scale, offsetX, offsetY));
        }
        else
        {
            child.Arrange(new Rect(finalSize));
        }

        return finalSize;
    }

    private Rect CalculateKeptArea(Size content)
    {
        Thickness margin = CropMargin;

        double width = content.Width - margin.Left - margin.Right;
        double height = content.Height - margin.Top - margin.Bottom;

        return new Rect(margin.Left, margin.Top, Math.Max(0, width), Math.Max(0, height));
    }

    private static double CalculateScale(Size finalSize, Size content, Rect kept)
    {
        double cover = Math.Max(finalSize.Width / content.Width, finalSize.Height / content.Height);
        double crop = Math.Min(Fit(finalSize.Width, kept.Width), Fit(finalSize.Height, kept.Height));

        return Math.Min(cover, crop);
    }

    private static double Fit(double viewport, double length)
    {
        if (length > 0)
        {
            return viewport / length;
        }
        else
        {
            return double.PositiveInfinity;
        }
    }

    private static double GetAnchor(HorizontalAlignment alignment)
    {
        return alignment switch
        {
            HorizontalAlignment.Left => 0,
            HorizontalAlignment.Right => 1,
            HorizontalAlignment.Center or HorizontalAlignment.Stretch => 0.5
        };
    }

    private static double GetAnchor(VerticalAlignment alignment)
    {
        return alignment switch
        {
            VerticalAlignment.Top => 0,
            VerticalAlignment.Bottom => 1,
            VerticalAlignment.Center or VerticalAlignment.Stretch => 0.5
        };
    }

    private static double CalculateOffset(
        double viewport,
        double content,
        double anchor,
        double keptOffset,
        double keptLength)
    {
        double anchored = (viewport - content) * anchor;

        double lower = double.NegativeInfinity;
        double upper = double.PositiveInfinity;

        if (keptLength > 0)
        {
            lower = -keptOffset;
            upper = viewport - keptOffset - keptLength;
        }

        if (content >= viewport)
        {
            lower = Math.Max(lower, viewport - content);
            upper = Math.Min(upper, 0);
        }

        if (upper < lower)
        {
            upper = lower;
        }

        return Math.Clamp(anchored, lower, upper);
    }
}
