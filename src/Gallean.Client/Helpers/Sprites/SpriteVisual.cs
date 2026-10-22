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

    private AssetCache<Sprite>.Lease? lease;
    private Sprite? sprite;

    static SpriteVisual()
    {
        AffectsRender<SpriteVisual>(SpriteKeyProperty, StretchProperty, StretchDirectionProperty);
        AffectsMeasure<SpriteVisual>(SpriteKeyProperty, StretchProperty, StretchDirectionProperty);
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

        return Stretch.CalculateSize(availableSize, SpriteSize(sprite), StretchDirection);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (sprite is null)
        {
            return default;
        }

        return Stretch.CalculateSize(finalSize, SpriteSize(sprite));
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (sprite is not null && Bounds is { Width: > 0, Height: > 0 })
        {
            Size sourceSize = SpriteSize(sprite);
            Rect viewPort = new(Bounds.Size);
            Vector scale = Stretch.CalculateScaling(Bounds.Size, sourceSize, StretchDirection);
            Size scaledSize = sourceSize * scale;
            Rect centered = viewPort.CenterRect(new Rect(scaledSize));
            Rect destination = centered.Intersect(viewPort);
            Rect logicalSource = new Rect(sourceSize).CenterRect(new Rect(destination.Size / scale));
            Rect source = ToPixelSpace(logicalSource, sprite);

            context.Custom(new SpriteRenderOperation(destination, source, sprite));
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
