using System.Diagnostics;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Gallean.Configuration;

namespace Gallean.Client;

internal sealed class BattleVisual : AbstractAnimatedVisual
{
    public static readonly StyledProperty<bool> IsLightOverlayVisibleProperty =
        AvaloniaProperty.Register<BattleVisual, bool>(nameof(IsLightOverlayVisible));

    public static readonly StyledProperty<bool> IsHitboxOverlayVisibleProperty =
        AvaloniaProperty.Register<BattleVisual, bool>(nameof(IsHitboxOverlayVisible));

    private readonly AnimationClock animationClock = new();
    private readonly BattleRenderContentProvider contentProvider = new();
    private readonly BattleRenderer renderer = new();
    private readonly AnimationPlayback<AnimationSequence> sequencePlayback = new();
    private readonly AnimationPlayback<BattleWorldTransitionState> transitionPlayback = new();
    private AnimationSoundCursor soundCursor = new([]);
    private BattleAnimationState? lastAnimationState;
    private BattleRenderState? lastRenderState;
    private Exception? renderException;
    private int lastFrameIndex = -1;
    private bool lastFrameDidRedraw;
    private long lastFrameTimestamp;

    static BattleVisual()
    {
        AffectsRender<BattleVisual>(IsLightOverlayVisibleProperty, IsHitboxOverlayVisibleProperty);
    }

    public BattleVisual()
    {
        Focusable = false;
    }

    public bool IsLightOverlayVisible
    {
        get => GetValue(IsLightOverlayVisibleProperty);
        set => SetValue(IsLightOverlayVisibleProperty, value);
    }

    public bool IsHitboxOverlayVisible
    {
        get => GetValue(IsHitboxOverlayVisibleProperty);
        set => SetValue(IsHitboxOverlayVisibleProperty, value);
    }

    private BattleScreenViewModel ViewModel => (BattleScreenViewModel)DataContext!;
    private IAssetProvider Assets => AssetScope.GetRequiredAssets(this);
    private IAudioPlayer Audio => AudioScope.GetRequiredAudio(this);
    private DiagnosticsRecorder Diagnostics => ViewModel.Game.DevTools.Diagnostics;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == SettingsScope.SettingsProperty)
        {
            InvalidateVisual();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        PointerPoint point = e.GetCurrentPoint(this);
        PointerPointProperties properties = point.Properties;
        BattleGridTile? tile = HitTestPointer(e);

        if (properties.IsRightButtonPressed)
        {
            if (tile is not null)
            {
                if (ViewModel.TryShowUnitDetails(tile))
                {
                    e.Handled = true;
                }
            }
        }
        else if (properties.IsLeftButtonPressed)
        {
            if (tile is not null)
            {
                ViewModel.ClickAsync(tile).FileAndForget();
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            ViewModel.HideUnitDetails();
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);

        ViewModel.HideUnitDetails();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        ViewModel.Hover(HitTestPointer(e));
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        ViewModel.Hover(null);
    }

    protected override void OnFrame(TimeSpan frameTime)
    {
        TimeSpan elapsedTime = TimeSpan.Zero;
        bool measure = Diagnostics.IsEnabled;

        if (measure)
        {
            long now = Stopwatch.GetTimestamp();

            if (lastFrameTimestamp != 0)
            {
                elapsedTime = Stopwatch.GetElapsedTime(lastFrameTimestamp, now);
            }

            lastFrameTimestamp = now;
        }
        else
        {
            lastFrameTimestamp = 0;
        }

        if (renderException is not null)
        {
            StopFrames();
            ViewModel.HandleRenderException(renderException);
            return;
        }

        bool didRedraw = false;

        try
        {
            TimeSpan now = animationClock.Now;
            BattleAnimationState? animation = ViewModel.AnimationState;

            RegisterSequence(animation?.Sequence, now);
            PlaySequenceSounds(animation, now);

            if (sequencePlayback.TryComplete(now))
            {
                ViewModel.CompleteAnimation(now);
            }

            transitionPlayback.Register(ViewModel.TransitionState, now);

            if (transitionPlayback.TryComplete(now))
            {
                ViewModel.CompleteTransitionAsync().FileAndForget();
            }

            bool redraw = false;

            int frameIndex = ViewModel.RenderState.RedrawRate.GetFrameIndex(now);

            if (sequencePlayback.IsPlaying)
            {
                redraw = true;
            }

            if (transitionPlayback.IsPlaying)
            {
                redraw = true;
            }

            if (frameIndex != lastFrameIndex)
            {
                redraw = true;
            }

            if (!ReferenceEquals(ViewModel.RenderState, lastRenderState))
            {
                redraw = true;
            }

            if (!ReferenceEquals(ViewModel.AnimationState, lastAnimationState))
            {
                redraw = true;
            }

            if (redraw)
            {
                didRedraw = true;
                lastFrameIndex = frameIndex;
                lastRenderState = ViewModel.RenderState;
                lastAnimationState = ViewModel.AnimationState;
                InvalidateVisual();
            }
        }
        finally
        {
            if (measure && elapsedTime > TimeSpan.Zero)
            {
                Diagnostics.AddFrame(elapsedTime, didRedraw && lastFrameDidRedraw);
            }

            lastFrameDidRedraw = didRedraw;
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        BattleRenderState state = ViewModel.RenderState;
        BattleAnimationState? animation = ViewModel.AnimationState;
        TimeSpan loopTime = animationClock.Now;

        BattleWorldTransitionState? transition = ViewModel.TransitionState;

        RegisterSequence(animation?.Sequence, loopTime);

        TimeSpan sequenceTime = GetSequenceTime(animation, loopTime);
        TimeSpan transitionTime = transitionPlayback.GetElapsed(loopTime);
        Matrix view = BattleRenderLayout.GetViewMatrix(Bounds.Width, Bounds.Height);

        IAssetProvider assets = Assets;
        BattleRenderContent content;

        try
        {
            content = contentProvider.Get(state, assets);
        }
        catch (Exception ex)
        {
            renderException = ex;
            return;
        }

        Rect bounds = GetSurfaceBounds();
        GameSettings settings = SettingsScope.GetSettings(this);

        context.Custom(new BattleRenderOperation(
            bounds,
            view,
            state,
            content,
            animation,
            transition,
            assets,
            renderer,
            loopTime,
            sequenceTime,
            transitionTime,
            settings.RenderResolutionRatio,
            IsLightOverlayVisible,
            IsHitboxOverlayVisible,
            Diagnostics));
    }

    private Rect GetSurfaceBounds()
    {
        Visual? parent = this.GetVisualParent();

        if (parent is not null)
        {
            return new Rect(-Bounds.X, -Bounds.Y, parent.Bounds.Width, parent.Bounds.Height);
        }
        else
        {
            return new Rect(Bounds.Size);
        }
    }

    private void RegisterSequence(AnimationSequence? sequence, TimeSpan now)
    {
        if (sequencePlayback.Register(sequence, now))
        {
            soundCursor = new AnimationSoundCursor(sequence?.Sounds ?? []);
        }
    }

    private void PlaySequenceSounds(BattleAnimationState? animation, TimeSpan now)
    {
        if (sequencePlayback.IsPlaying)
        {
            TimeSpan sequenceTime = GetSequenceTime(animation, now);

            soundCursor.Play(sequenceTime, Audio);
        }
    }

    private TimeSpan GetSequenceTime(BattleAnimationState? animation, TimeSpan now)
    {
        if (animation is not null)
        {
            return animation.Sequence.GetTime(sequencePlayback.GetElapsed(now));
        }
        else
        {
            return TimeSpan.Zero;
        }
    }

    private BattleGridTile? HitTestPointer(PointerEventArgs e)
    {
        Point scene = BattleRenderLayout.ScreenToScene(e.GetPosition(this), Bounds.Width, Bounds.Height);

        return BattleHitTester.HitTest(ViewModel.RenderState.HitTargets, scene);
    }
}
