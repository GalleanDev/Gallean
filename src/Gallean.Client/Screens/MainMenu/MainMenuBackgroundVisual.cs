using Avalonia;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed partial class MainMenuBackgroundVisual : AbstractAnimatedVisual
{
    private static readonly MainMenuBackgroundLighting Lighting = MainMenuBackgroundLighting.Create();
    private static readonly AnimationClock LightClock = new();
    private static TimeSpan lightTime;

    private AssetCache<Sprite>.Lease? lease;
    private Sprite? sprite;

    static MainMenuBackgroundVisual()
    {
        AffectsRender<MainMenuBackgroundVisual>(SpriteKeyProperty);
        AffectsMeasure<MainMenuBackgroundVisual>(SpriteKeyProperty);
    }

    public MainMenuBackgroundVisual()
    {
        ClipToBounds = true;
    }

    [GeneratedStyledProperty]
    public partial AssetSpriteKey? SpriteKey { get; set; }

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

    protected override void OnFrame(TimeSpan frameTime)
    {
        lightTime += LightClock.Tick();
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (sprite is null)
        {
            return default;
        }

        return Stretch.Fill.CalculateSize(availableSize, SpriteSize(sprite));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (sprite is null)
        {
            return default;
        }

        return Stretch.Fill.CalculateSize(finalSize, SpriteSize(sprite));
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (sprite is not null && Bounds is { Width: > 0, Height: > 0 })
        {
            Rect destination = new(Bounds.Size);

            if (sprite.Normal is not null &&
                sprite.Depth is not null &&
                sprite.Albedo is not null)
            {
                context.Custom(new MainMenuBackgroundRenderOperation(destination, sprite, sprite.Normal, sprite.Depth, sprite.Albedo, Lighting, lightTime));
            }
            else
            {
                context.Custom(new SpriteRenderOperation(destination, new Rect(0, 0, sprite.Image.Width, sprite.Image.Height), sprite, RenderSamplingOptions.Linear));
            }
        }
    }

    private static Size SpriteSize(Sprite sprite)
    {
        return new Size(sprite.Metadata.Width, sprite.Metadata.Height);
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
}
