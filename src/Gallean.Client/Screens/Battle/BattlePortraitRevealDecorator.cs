using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed class BattlePortraitRevealDecorator : Decorator
{
    private const double Rise = 16.0;

    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(0.3);

    public static readonly StyledProperty<BattleUnitId?> UnitIdProperty =
        AvaloniaProperty.Register<BattlePortraitRevealDecorator, BattleUnitId?>(nameof(UnitId));

    private readonly TranslateTransform transform = new();

    private TopLevel? topLevel;
    private TimeSpan? started;
    private bool revealing;
    private bool frameRequested;

    public BattlePortraitRevealDecorator()
    {
        RenderTransform = transform;
    }

    public BattleUnitId? UnitId
    {
        get => GetValue(UnitIdProperty);
        set => SetValue(UnitIdProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == UnitIdProperty && UnitId is not null)
        {
            Reveal();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        topLevel = TopLevel.GetTopLevel(this);
        started = null;

        if (revealing)
        {
            RequestFrame();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        topLevel = null;
    }

    private void Reveal()
    {
        revealing = true;
        started = null;
        ApplyProgress(0);
        RequestFrame();
    }

    private void RequestFrame()
    {
        if (!frameRequested && topLevel is not null)
        {
            frameRequested = true;
            topLevel.RequestAnimationFrame(OnFrame);
        }
    }

    private void OnFrame(TimeSpan time)
    {
        frameRequested = false;

        if (topLevel is not null && revealing)
        {
            started ??= time;

            double progress = time.ToAnimationProgress(started.Value, Duration);
            if (progress < 1)
            {
                RequestFrame();
            }
            else
            {
                revealing = false;
            }

            ApplyProgress(progress);
        }
    }

    private void ApplyProgress(double progress)
    {
        double eased = AnimationEasing.Decelerate.Ease(progress);

        Opacity = eased;
        transform.Y = Rise * (1 - eased);
    }
}
