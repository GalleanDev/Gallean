using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed class CapitalBackdropVisual : Control
{
    private const double BlurStrength = 30;
    private const double EdgeFadeWidth = 0.015;

    public static readonly StyledProperty<AssetSpriteKey.CapitalBackground?> SpriteKeyProperty =
        AvaloniaProperty.Register<CapitalBackdropVisual, AssetSpriteKey.CapitalBackground?>(nameof(SpriteKey));

    private AssetCache<Sprite>.Lease? lease;
    private Sprite? sprite;

    static CapitalBackdropVisual()
    {
        AffectsRender<CapitalBackdropVisual>(SpriteKeyProperty);
    }

    public CapitalBackdropVisual()
    {
        ClipToBounds = true;
    }

    public AssetSpriteKey.CapitalBackground? SpriteKey
    {
        get => GetValue(SpriteKeyProperty);
        set => SetValue(SpriteKeyProperty, value);
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
            Size sourceSize = new(sprite.Metadata.Width, sprite.Metadata.Height);
            Rect bounds = new(Bounds.Size);
            Vector scale = Stretch.Uniform.CalculateScaling(Bounds.Size, sourceSize);
            Rect viewport = bounds.CenterRect(new Rect(sourceSize * scale));

            context.Custom(new CapitalBackdropRenderOperation(bounds, viewport, sprite, BlurStrength, EdgeFadeWidth));
        }
    }
}
