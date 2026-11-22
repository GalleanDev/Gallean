using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering;
using Gallean.Common;

namespace Gallean.Client;

internal sealed class AnimationVisual : AbstractAnimatedVisual, ICustomHitTest
{
    public static readonly StyledProperty<AssetAnimationKey?> AnimationKeyProperty =
        AvaloniaProperty.Register<AnimationVisual, AssetAnimationKey?>(nameof(AnimationKey));

    public static readonly StyledProperty<Stretch> StretchProperty =
        AvaloniaProperty.Register<AnimationVisual, Stretch>(nameof(Stretch), Stretch.Uniform);

    public static readonly StyledProperty<StretchDirection> StretchDirectionProperty =
        AvaloniaProperty.Register<AnimationVisual, StretchDirection>(nameof(StretchDirection), StretchDirection.Both);

    private Animation? animation;
    private int frameIndex;
    private AssetCache<Animation>.Lease? lease;

    static AnimationVisual()
    {
        AffectsRender<AnimationVisual>(AnimationKeyProperty, StretchProperty, StretchDirectionProperty);
        AffectsMeasure<AnimationVisual>(AnimationKeyProperty, StretchProperty, StretchDirectionProperty);
    }

    public AnimationVisual()
    {
        ClipToBounds = true;
    }

    public AssetAnimationKey? AnimationKey
    {
        get => GetValue(AnimationKeyProperty);
        set => SetValue(AnimationKeyProperty, value);
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

        if (e.Property == AnimationKeyProperty || e.Property == AssetScope.AssetsProperty)
        {
            RefreshAnimation();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        RefreshAnimation();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        lease?.Dispose();
        lease = null;
        animation = null;
    }

    private void RefreshAnimation()
    {
        lease?.Dispose();

        IAssetProvider? assets = AssetScope.GetOptionalAssets(this);

        if (AnimationKey is not null && assets is not null)
        {
            lease = assets.GetOptionalAnimation(AnimationKey);
            animation = lease?.Value;
        }
        else
        {
            lease = null;
            animation = null;
        }
    }

    protected override void OnFrame(TimeSpan frameTime)
    {
        int frame = AnimationRate.Sprites.GetFrameIndex(frameTime);

        if (frame != frameIndex)
        {
            frameIndex = frame;
            InvalidateVisual();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (animation is null)
        {
            return default;
        }

        return Stretch.CalculateSize(availableSize, AnimationSize(animation), StretchDirection);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (animation is null)
        {
            return default;
        }

        return Stretch.CalculateSize(finalSize, AnimationSize(animation));
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (animation is not null && Bounds is { Width: > 0, Height: > 0 })
        {
            Sprite frame = animation.FrameByIndex(frameIndex, looping: true);
            Size boxSize = AnimationSize(animation);
            Rect viewport = new(Bounds.Size);
            Vector scale = Stretch.CalculateScaling(Bounds.Size, boxSize, StretchDirection);
            Rect box = viewport.CenterRect(new Rect(boxSize * scale));

            Rect frameDestination = new(
                box.X + ((frame.Metadata.OffsetX - animation.Metadata.OffsetX) * scale.X),
                box.Y + ((frame.Metadata.OffsetY - animation.Metadata.OffsetY) * scale.Y),
                frame.Metadata.Width * scale.X,
                frame.Metadata.Height * scale.Y);

            Rect destination = frameDestination.Intersect(viewport);

            if (destination is { Width: > 0, Height: > 0 })
            {
                Rect logicalSource = new(
                    (destination.X - frameDestination.X) / scale.X,
                    (destination.Y - frameDestination.Y) / scale.Y,
                    destination.Width / scale.X,
                    destination.Height / scale.Y);

                context.Custom(new SpriteRenderOperation(destination, ToPixelSpace(logicalSource, frame), frame));
            }
        }
    }

    public bool HitTest(Point point)
    {
        if (animation?.HitMask is not null && Bounds is { Width: > 0, Height: > 0 })
        {
            ImmutableBitmap mask = animation.HitMask;
            Size boxSize = AnimationSize(animation);
            Rect viewport = new(Bounds.Size);
            Vector scale = Stretch.CalculateScaling(Bounds.Size, boxSize, StretchDirection);
            Rect box = viewport.CenterRect(new Rect(boxSize * scale));
            int column = (int)Math.Floor((point.X - box.X) / scale.X);
            int row = (int)Math.Floor((point.Y - box.Y) / scale.Y);

            if (column >= 0 && row >= 0 && column < mask.Width && row < mask.Height)
            {
                return mask.Get(column, row);
            }
        }

        return false;
    }

    private static Size AnimationSize(Animation animation)
    {
        return new Size(animation.Metadata.Width, animation.Metadata.Height);
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
