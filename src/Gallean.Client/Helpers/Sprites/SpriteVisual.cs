using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SkiaSharp;

namespace Gallean.Client;

internal sealed partial class SpriteVisual : Control
{
    private AssetCache<Sprite>.Lease? lease;
    private Sprite? sprite;

    static SpriteVisual()
    {
        AffectsRender<SpriteVisual>(SpriteKeyProperty, StretchProperty, StretchDirectionProperty, CanvasSizeProperty);
        AffectsMeasure<SpriteVisual>(SpriteKeyProperty, StretchProperty, StretchDirectionProperty, CanvasSizeProperty);
    }

    public SpriteVisual()
    {
        ClipToBounds = true;
    }

    [GeneratedStyledProperty]
    public partial AssetSpriteKey? SpriteKey { get; set; }

    [GeneratedStyledProperty(DefaultValue = Stretch.Uniform)]
    public partial Stretch Stretch { get; set; }

    [GeneratedStyledProperty(DefaultValue = StretchDirection.Both)]
    public partial StretchDirection StretchDirection { get; set; }

    [GeneratedStyledProperty]
    public partial Size? CanvasSize { get; set; }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == SpriteKeyProperty || e.Property == AssetScope.AssetsProperty)
        {
            RefreshSprite();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        RefreshSprite();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        lease?.Dispose();
        lease = null;
        sprite = null;
    }

    private void RefreshSprite()
    {
        lease?.Dispose();

        IAssetProvider? assets = AssetScope.GetAssets(this);

        if (SpriteKey is not null && assets is not null)
        {
            lease = assets.GetOptionalSprite(SpriteKey);
            sprite = lease?.Value;
        }
        else
        {
            lease = null;
            sprite = null;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (sprite is null)
        {
            return default;
        }

        return Stretch.CalculateSize(availableSize, CanvasSize ?? SpriteSize(sprite), StretchDirection);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (sprite is null)
        {
            return default;
        }

        return Stretch.CalculateSize(finalSize, CanvasSize ?? SpriteSize(sprite));
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (sprite is not null && Bounds is { Width: > 0, Height: > 0 })
        {
            if (CanvasSize is not null)
            {
                Size canvasSize = CanvasSize.Value;
                Rect viewport = new(Bounds.Size);
                Vector scale = Stretch.CalculateScaling(Bounds.Size, canvasSize, StretchDirection);
                Rect canvas = viewport.CenterRect(new Rect(canvasSize * scale));
                double left = sprite.Metadata.OffsetX;
                double right = left + sprite.Metadata.Width;
                double halfPixel = sprite.Metadata.Width / (sprite.Image.Width * 2.0);

                List<(double Left, double Right, SKSamplingOptions Sampling)> spans = [];

                if (left < 0)
                {
                    spans.Add((left, -halfPixel, RenderSamplingOptions.Linear));
                    spans.Add((-halfPixel, 0, RenderSamplingOptions.Nearest));
                }

                spans.Add((Math.Max(left, 0), Math.Min(right, canvasSize.Width), RenderSamplingOptions.Linear));

                if (right > canvasSize.Width)
                {
                    spans.Add((canvasSize.Width, canvasSize.Width + halfPixel, RenderSamplingOptions.Nearest));
                    spans.Add((canvasSize.Width + halfPixel, right, RenderSamplingOptions.Linear));
                }

                foreach ((double spanLeft, double spanRight, SKSamplingOptions sampling) in spans)
                {
                    Rect destination = new(
                        canvas.X + (spanLeft * scale.X),
                        canvas.Y + (sprite.Metadata.OffsetY * scale.Y),
                        (spanRight - spanLeft) * scale.X,
                        sprite.Metadata.Height * scale.Y);

                    Rect source = new(spanLeft - sprite.Metadata.OffsetX, 0, spanRight - spanLeft, sprite.Metadata.Height);

                    context.Custom(new SpriteRenderOperation(destination, ToPixelSpace(source, sprite), sprite, sampling));
                }
            }
            else
            {
                Size sourceSize = SpriteSize(sprite);
                Rect viewport = new(Bounds.Size);
                Vector scale = Stretch.CalculateScaling(Bounds.Size, sourceSize, StretchDirection);
                Size scaledSize = sourceSize * scale;
                Rect centered = viewport.CenterRect(new Rect(scaledSize));
                Rect destination = centered.Intersect(viewport);
                Rect logicalSource = new Rect(sourceSize).CenterRect(new Rect(destination.Size / scale));
                Rect source = ToPixelSpace(logicalSource, sprite);

                context.Custom(new SpriteRenderOperation(destination, source, sprite, RenderSamplingOptions.Linear));
            }
        }
    }

    private static Size SpriteSize(Sprite sprite)
    {
        return new Size(sprite.Metadata.Width, sprite.Metadata.Height);
    }

    private static Rect ToPixelSpace(Rect logicalSource, Sprite sprite)
    {
        double scaleX = sprite.Image.Width / (double)sprite.Metadata.Width;
        double scaleY = sprite.Image.Height / (double)sprite.Metadata.Height;

        return new Rect(
            logicalSource.X * scaleX,
            logicalSource.Y * scaleY,
            logicalSource.Width * scaleX,
            logicalSource.Height * scaleY);
    }
}
