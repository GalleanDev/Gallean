using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed class CapitalBackdropVisual : Control
{
    private const double BlurStrength = 30;
    private const double EdgeFadeWidth = 0.015;

    public static readonly StyledProperty<AssetSpriteKey?> SpriteKeyProperty =
        AvaloniaProperty.Register<CapitalBackdropVisual, AssetSpriteKey?>(nameof(SpriteKey));

    public static readonly StyledProperty<Size> CanvasSizeProperty =
        AvaloniaProperty.Register<CapitalBackdropVisual, Size>(nameof(CanvasSize));

    private AssetCache<Sprite>.Lease? lease;
    private Sprite? sprite;

    static CapitalBackdropVisual()
    {
        AffectsRender<CapitalBackdropVisual>(SpriteKeyProperty, CanvasSizeProperty);
    }

    public CapitalBackdropVisual()
    {
        ClipToBounds = true;
    }

    public AssetSpriteKey? SpriteKey
    {
        get => GetValue(SpriteKeyProperty);
        set => SetValue(SpriteKeyProperty, value);
    }

    public Size CanvasSize
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

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (sprite is not null && Bounds is { Width: > 0, Height: > 0 })
        {
            Rect bounds = new(Bounds.Size);
            Vector scale = Stretch.Uniform.CalculateScaling(Bounds.Size, CanvasSize);
            Rect canvas = bounds.CenterRect(new Rect(CanvasSize * scale));

            Rect viewport = new(
                canvas.X + (sprite.Metadata.OffsetX * scale.X),
                canvas.Y + (sprite.Metadata.OffsetY * scale.Y),
                sprite.Metadata.Width * scale.X,
                sprite.Metadata.Height * scale.Y);

            context.Custom(new CapitalBackdropRenderOperation(bounds, viewport, sprite, BlurStrength, EdgeFadeWidth));
        }
    }
}
