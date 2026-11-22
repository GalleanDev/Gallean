using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed class SpriteVisual : Control
{
    public static readonly StyledProperty<AssetSpriteKey?> SpriteKeyProperty =
        AvaloniaProperty.Register<SpriteVisual, AssetSpriteKey?>(nameof(SpriteKey));

    public static readonly StyledProperty<Stretch> StretchProperty =
        AvaloniaProperty.Register<SpriteVisual, Stretch>(nameof(Stretch), Stretch.Uniform);

    public static readonly StyledProperty<StretchDirection> StretchDirectionProperty =
        AvaloniaProperty.Register<SpriteVisual, StretchDirection>(nameof(StretchDirection), StretchDirection.Both);

    public static readonly StyledProperty<Size?> CanvasSizeProperty =
        AvaloniaProperty.Register<SpriteVisual, Size?>(nameof(CanvasSize));

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

    public AssetSpriteKey? SpriteKey
    {
        get => GetValue(SpriteKeyProperty);
        set => SetValue(SpriteKeyProperty, value);
    }

    public Stretch Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    public StretchDirection StretchDirection
    {
        get => GetValue(StretchDirectionProperty);
        set => SetValue(StretchDirectionProperty, value);
    }

    public Size? CanvasSize
    {
        get => GetValue(CanvasSizeProperty);
        set => SetValue(CanvasSizeProperty, value);
    }

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

        IAssetProvider? assets = AssetScope.GetOptionalAssets(this);

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
                Rect viewport = new(Bounds.Size);
                Vector scale = Stretch.CalculateScaling(Bounds.Size, CanvasSize.Value, StretchDirection);
                Rect canvas = viewport.CenterRect(new Rect(CanvasSize.Value * scale));

                Rect destination = new(
                    canvas.X + (sprite.Metadata.OffsetX * scale.X),
                    canvas.Y + (sprite.Metadata.OffsetY * scale.Y),
                    sprite.Metadata.Width * scale.X,
                    sprite.Metadata.Height * scale.Y);

                Rect source = new(0, 0, sprite.Image.Width, sprite.Image.Height);

                context.Custom(new SpriteRenderOperation(destination, source, sprite));
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

                context.Custom(new SpriteRenderOperation(destination, source, sprite));
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
