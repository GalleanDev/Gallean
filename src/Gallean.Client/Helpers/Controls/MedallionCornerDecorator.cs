using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Gallean.Client;

internal sealed class MedallionCornerDecorator : Decorator
{
    private const double StartAngle = 90;
    private const double EndAngle = 180;

    private readonly Image front = new() { Stretch = Stretch.Fill, IsHitTestVisible = false, ZIndex = 1 };

    private WriteableBitmap? back;
    private Size textureSize;
    private Matrix textureTransform;

    static MedallionCornerDecorator()
    {
        ClipToBoundsProperty.OverrideDefaultValue<MedallionCornerDecorator>(false);
    }

    public MedallionCornerDecorator()
    {
        VisualChildren.Add(front);
    }

    private void UpdateTextures(Size size, Matrix transform, Point center)
    {
        if (back is null || textureSize != size || textureTransform != transform)
        {
            Point rightRhombus = transform.Transform(OrnamentShape.Polar(center, MedallionCornerTexture.RhombusRadius, StartAngle));
            Point bottomRhombus = transform.Transform(OrnamentShape.Polar(center, MedallionCornerTexture.RhombusRadius, EndAngle));
            IImage? previousFront = front.Source;

            back?.Dispose();
            back = MedallionCornerTexture.Create(MedallionCornerLayer.Back, size, transform, center, rightRhombus, bottomRhombus);
            front.Source = MedallionCornerTexture.Create(MedallionCornerLayer.Front, size, transform, center, rightRhombus, bottomRhombus);
            (previousFront as IDisposable)?.Dispose();
            textureSize = size;
            textureTransform = transform;
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Size size = base.ArrangeOverride(finalSize);
        Control? child = Child;

        front.Arrange(new Rect(size));

        if (child is not null)
        {
            Matrix? transform = child.TransformToVisual(this);

            if (transform is not null)
            {
                UpdateTextures(size, transform.Value, new Point(child.Bounds.Width / 2.0, child.Bounds.Height / 2.0));
            }
        }

        return size;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        front.Measure(availableSize);

        return base.MeasureOverride(availableSize);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        back?.Dispose();
        back = null;
        (front.Source as IDisposable)?.Dispose();
        front.Source = null;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (back is not null)
        {
            context.DrawImage(back, new Rect(Bounds.Size));
        }
    }
}
