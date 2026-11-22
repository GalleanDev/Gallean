using Avalonia;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed partial class VideoVisual : AbstractAnimatedVisual
{
    public static readonly StyledProperty<AssetAnimationKey?> AnimationKeyProperty =
        AvaloniaProperty.Register<VideoVisual, AssetAnimationKey?>(nameof(AnimationKey));

    public static readonly StyledProperty<AnimationRate> RateProperty =
        AvaloniaProperty.Register<VideoVisual, AnimationRate>(nameof(Rate), AnimationRate.Sprites);

    public static readonly StyledProperty<TimeSpan> StartTimeProperty =
        AvaloniaProperty.Register<VideoVisual, TimeSpan>(nameof(StartTime));

    public static readonly StyledProperty<Stretch> StretchProperty =
        AvaloniaProperty.Register<VideoVisual, Stretch>(nameof(Stretch), Stretch.Uniform);

    public static readonly StyledProperty<double> GrainProperty =
        AvaloniaProperty.Register<VideoVisual, double>(nameof(Grain));

    private readonly FrameBuffer frameBuffer = new();
    private Playback? playback;

    static VideoVisual()
    {
        AffectsRender<VideoVisual>(AnimationKeyProperty, StretchProperty, GrainProperty);
        AffectsMeasure<VideoVisual>(AnimationKeyProperty, StretchProperty);
    }

    public VideoVisual()
    {
        ClipToBounds = true;
    }

    public AssetAnimationKey? AnimationKey
    {
        get => GetValue(AnimationKeyProperty);
        set => SetValue(AnimationKeyProperty, value);
    }

    public AnimationRate Rate
    {
        get => GetValue(RateProperty);
        set => SetValue(RateProperty, value);
    }

    public TimeSpan StartTime
    {
        get => GetValue(StartTimeProperty);
        set => SetValue(StartTimeProperty, value);
    }

    public Stretch Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    public double Grain
    {
        get => GetValue(GrainProperty);
        set => SetValue(GrainProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == AnimationKeyProperty ||
            e.Property == StartTimeProperty ||
            e.Property == AssetScope.AssetsProperty)
        {
            Restart();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        Restart();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        frameBuffer.Clear();
        playback = null;
    }

    private void Restart()
    {
        frameBuffer.Clear();
        playback = CreatePlayback();

        InvalidateMeasure();
    }

    private Playback? CreatePlayback()
    {
        IAssetProvider? assets = AssetScope.GetOptionalAssets(this);

        if (AnimationKey is null || assets is null)
        {
            return null;
        }

        AssetAnimationMetadata? metadata = assets.GetOptionalAnimationMetadata(AnimationKey);

        if (metadata is null)
        {
            return null;
        }

        return new Playback(assets, AnimationKey, metadata.Value);
    }

    protected override void OnFrame(TimeSpan frameTime)
    {
        if (playback is null)
        {
            return;
        }

        if (playback.ClockStart is null)
        {
            playback.ClockStart = frameTime;
        }

        int rateIndex = Rate.GetFrameIndex(StartTime + (frameTime - playback.ClockStart.Value));
        int index = Math.Min(rateIndex, playback.Metadata.FrameCount - 1);

        if (index != playback.FrameIndex && !playback.IsLoading)
        {
            LoadFrameAsync(playback, index).FileAndForget();
        }
    }

    private async Task LoadFrameAsync(Playback started, int index)
    {
        Sprite? sprite;

        started.IsLoading = true;

        try
        {
            sprite = await Task.Run(() => started.Assets.GetAnimationFrame(started.Key, index));
        }
        finally
        {
            started.IsLoading = false;
        }

        if (started == playback)
        {
            started.FrameIndex = index;

            if (sprite is not null)
            {
                frameBuffer.Present(sprite.Image);
                started.FrameSize = new PixelSize(sprite.Image.Width, sprite.Image.Height);
            }
            else
            {
                frameBuffer.Clear();
                started.FrameSize = null;
            }

            InvalidateVisual();
        }
        else
        {
            sprite?.Image.Dispose();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (playback is not null)
        {
            return Stretch.CalculateSize(availableSize, VideoSize(playback.Metadata));
        }
        else
        {
            return default;
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (playback is not null)
        {
            return Stretch.CalculateSize(finalSize, VideoSize(playback.Metadata));
        }
        else
        {
            return default;
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        PixelSize? frameSize = playback?.FrameSize;

        if (playback is not null && frameSize is not null && Bounds is { Width: > 0, Height: > 0 })
        {
            Size videoSize = VideoSize(playback.Metadata);
            Rect viewport = new(Bounds.Size);
            Vector scale = Stretch.CalculateScaling(Bounds.Size, videoSize);
            Rect box = viewport.CenterRect(new Rect(videoSize * scale));
            Rect destination = box.Intersect(viewport);

            if (destination is { Width: > 0, Height: > 0 })
            {
                double pixelScaleX = frameSize.Value.Width / (videoSize.Width * scale.X);
                double pixelScaleY = frameSize.Value.Height / (videoSize.Height * scale.Y);

                Rect source = new(
                    (destination.X - box.X) * pixelScaleX,
                    (destination.Y - box.Y) * pixelScaleY,
                    destination.Width * pixelScaleX,
                    destination.Height * pixelScaleY);

                context.Custom(new RenderOperation(destination, source, frameBuffer, playback.FrameIndex, Grain));
            }
        }
    }

    private static Size VideoSize(AssetAnimationMetadata metadata)
    {
        return new Size(metadata.Width, metadata.Height);
    }
}
