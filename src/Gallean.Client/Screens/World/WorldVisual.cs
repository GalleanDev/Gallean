using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using CommunityToolkit.Mvvm.Input;
using Gallean.Common;
using Gallean.Configuration;

namespace Gallean.Client;

internal sealed partial class WorldVisual : AbstractAnimatedVisual
{
    private const double CameraSpeed = 2000.0;
    private const double ZoomStep = 0.12;
    private const double FastForwardRate = 2.0;
    private const double InterfaceScrollMargin = 4.0;

    public static readonly StyledProperty<bool> IsHitboxOverlayVisibleProperty =
        AvaloniaProperty.Register<WorldVisual, bool>(nameof(IsHitboxOverlayVisible));

    private static readonly ImmutableSolidColorBrush BackdropBrush = new(Client.Theme.World.BackdropColor);

    private readonly WorldAmbientSoundController ambientSoundController = new();
    private readonly AnimationClock animationClock = new();
    private readonly PlaybackClock playbackClock = new();
    private readonly AnimationPlayback<WorldRenderState.WorldAnimation> animationPlayback = new();
    private readonly HashSet<Key> pressedKeys = [];
    private readonly WorldRenderer renderer = new();
    private readonly ScrollHelper scrollHelper = new();
    private WorldScreenSnapshotWorker snapshotWorker = null!;
    private WorldCamera camera = null!;
    private Point? panAnchor;
    private CleanupList? cleanupList;
    private bool redrawRequested = true;
    private bool lastFrameDidRedraw;
    private int lastFrameIndex = -1;
    private long lastFrameTimestamp;
    private WorldScreenSnapshot? lastSnapshot;

    static WorldVisual()
    {
        AffectsRender<WorldVisual>(IsHitboxOverlayVisibleProperty);
    }

    public WorldVisual()
    {
        Focusable = false;
        ClipToBounds = true;
    }

    public bool IsHitboxOverlayVisible
    {
        get => GetValue(IsHitboxOverlayVisibleProperty);
        set => SetValue(IsHitboxOverlayVisibleProperty, value);
    }

    private WorldScreenViewModel ViewModel => (WorldScreenViewModel)DataContext!;
    private IAssetProvider Assets => AssetScope.GetRequiredAssets(this);
    private IAudioPlayer Audio => AudioScope.GetRequiredAudio(this);
    private DiagnosticsRecorder Diagnostics => ViewModel.Game.DevTools.Diagnostics;

    public void Attach(WorldCamera worldCamera, WorldScreenSnapshotWorker worker)
    {
        camera = worldCamera;
        snapshotWorker = worker;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        CleanupList cleanup = new();
        cleanupList = cleanup;

        TopLevel? topLevel = TopLevel.GetTopLevel(this);

        if (topLevel is not null)
        {
            cleanup.Add(topLevel.AddDisposableHandler(PointerMovedEvent, OnTopLevelPointerMoved, RoutingStrategies.Bubble, true));
            cleanup.Add(topLevel.AddDisposableHandler(PointerExitedEvent, OnTopLevelPointerExited, RoutingStrategies.Direct, true));
            cleanup.Add(topLevel.AddDisposableHandler(PointerPressedEvent, OnTopLevelPointerPressed, RoutingStrategies.Tunnel, true));
            cleanup.Add(topLevel.AddDisposableHandler(KeyDownEvent, OnTopLevelPreviewKeyDown, RoutingStrategies.Tunnel));
            cleanup.Add(topLevel.AddDisposableHandler(KeyDownEvent, OnTopLevelKeyDown, RoutingStrategies.Bubble, true));
            cleanup.Add(topLevel.AddDisposableHandler(KeyUpEvent, OnTopLevelKeyUp, RoutingStrategies.Bubble, true));

            topLevel.LostFocus += OnLostFocus;
            cleanup.Add(new ActionDisposable(() => topLevel.LostFocus -= OnLostFocus));
        }

        if (topLevel is Window window)
        {
            window.Deactivated += OnWindowDeactivated;
            cleanup.Add(new ActionDisposable(() => window.Deactivated -= OnWindowDeactivated));
        }

        snapshotWorker.Request(ViewModel.RenderRequest);

        WorldScreenViewModel viewModel = ViewModel;
        viewModel.CameraFocusRequested += OnCameraFocusRequested;
        cleanup.Add(new ActionDisposable(() => viewModel.CameraFocusRequested -= OnCameraFocusRequested));

        UpdateCameraBounds();

        if (ViewModel.CameraFocus is not null)
        {
            camera.LookAt(WorldTransform.ToWorldPoint(ViewModel.CameraFocus.Value));
        }

        redrawRequested = true;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        cleanupList?.Dispose();
        cleanupList = null;

        pressedKeys.Clear();
        scrollHelper.HandleInputReleased();
        ambientSoundController.Stop();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        camera.ViewportSize = e.NewSize;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        if (!ViewModel.IsActive)
        {
            return;
        }

        if (e.Delta.Y != 0)
        {
            double step = e.Delta.Y > 0 ? ZoomStep : -ZoomStep;
            camera.ZoomTo(camera.TargetZoom + step, e.GetPosition(this));

            redrawRequested = true;
            e.Handled = true;
        }
    }

    private void OnTopLevelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel.FastForwardCommand.CanExecute(null))
        {
            ViewModel.FastForwardCommand.Execute(null);
        }
    }

    private void OnTopLevelPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel.IsActive)
        {
            IRelayCommand[] commands = GetHotkeyCommands(e.Key, e.KeyModifiers);

            if (commands.Length > 0)
            {
                IRelayCommand? command = Array.Find(commands, candidate => candidate.CanExecute(null));
                command?.Execute(null);

                e.Handled = true;
            }
            else if (GetHotkeyStackIndex(e.Key, e.KeyModifiers) is { } stackIndex)
            {
                SelectStackItem(stackIndex);

                e.Handled = true;
            }
        }
    }

    private void SelectStackItem(int index)
    {
        if (index < ViewModel.StackItems.Count)
        {
            WorldStackId stackId = ViewModel.StackItems[index].StackId;

            if (ViewModel.SelectStackItemCommand.CanExecute(stackId))
            {
                ViewModel.SelectStackItemCommand.Execute(stackId);
            }
        }
    }

    private IRelayCommand[] GetHotkeyCommands(Key key, KeyModifiers modifiers)
    {
        return (key, modifiers) switch
        {
            (Key.Tab or Key.N, KeyModifiers.Shift) => [ViewModel.SelectPreviousStackCommand],
            (Key.Tab or Key.N, KeyModifiers.None) => [ViewModel.SelectNextStackCommand],
            (Key.T, KeyModifiers.None) => [ViewModel.InspectStackCommand],
            (Key.Space, KeyModifiers.None) => [ViewModel.FastForwardCommand],
            (Key.Enter, KeyModifiers.None) => [ViewModel.FastForwardCommand, ViewModel.FinishStackCommand],
            (Key.Enter, KeyModifiers.Control) => [ViewModel.EndTurnCommand],
            (Key.E, KeyModifiers.None) => [ViewModel.EndTurnCommand],
            (Key.C, KeyModifiers.None) => [ViewModel.OpenCapitalCommand],
            (Key.B, KeyModifiers.None) => [ViewModel.OpenSpellbookCommand],
            (Key.J, KeyModifiers.None) => [ViewModel.OpenQuestCommand],
            (Key.P, KeyModifiers.None) => [ViewModel.OpenStackCommand],
            _ => []
        };
    }

    private static int? GetHotkeyStackIndex(Key key, KeyModifiers modifiers)
    {
        return (key, modifiers) switch
        {
            (Key.D1 or Key.NumPad1, KeyModifiers.None) => 0,
            (Key.D2 or Key.NumPad2, KeyModifiers.None) => 1,
            (Key.D3 or Key.NumPad3, KeyModifiers.None) => 2,
            (Key.D4 or Key.NumPad4, KeyModifiers.None) => 3,
            (Key.D5 or Key.NumPad5, KeyModifiers.None) => 4,
            _ => null
        };
    }

    private void OnTopLevelKeyDown(object? sender, KeyEventArgs e)
    {
        pressedKeys.Add(e.Key);
    }

    private void OnTopLevelKeyUp(object? sender, KeyEventArgs e)
    {
        pressedKeys.Remove(e.Key);
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        ReleaseInput();
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        ReleaseInput();
    }

    private void ReleaseInput()
    {
        pressedKeys.Clear();
        scrollHelper.HandleInputReleased();
        panAnchor = null;

        ViewModel.HidePositionInfo();
    }

    private void OnCameraFocusRequested(WorldPosition position, TimeSpan duration)
    {
        camera.FocusOn(WorldTransform.ToWorldPoint(position), duration);
        redrawRequested = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!ViewModel.IsActive)
        {
            return;
        }

        PointerPoint point = e.GetCurrentPoint(this);
        Point worldPoint = GetWorldPoint(e);
        IReadOnlyList<WorldRenderContent.HitTarget> hitTargets = snapshotWorker.Current?.WorldContent.HitTargets ?? [];
        WorldPosition? hitPosition = WorldHitTester.HitTest(hitTargets, worldPoint);

        if (point.Properties.IsRightButtonPressed)
        {
            ViewModel.ShowPositionInfo(WorldTransform.FromWorldPoint(worldPoint), hitPosition);

            e.Handled = true;
        }
        else if (point.Properties.IsLeftButtonPressed)
        {
            ViewModel.HandleMapClickAsync(WorldTransform.FromWorldPoint(worldPoint), hitPosition, e.ClickCount).FileAndForget();
        }
        else if (point.Properties.IsMiddleButtonPressed)
        {
            panAnchor = worldPoint;
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            ViewModel.HidePositionInfo();
        }
        else if (e.InitialPressMouseButton == MouseButton.Middle)
        {
            panAnchor = null;
            e.Pointer.Capture(null);
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);

        panAnchor = null;

        ViewModel.HidePositionInfo();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!ViewModel.IsActive)
        {
            return;
        }

        bool measure = Diagnostics.IsEnabled;
        long moveStartTimestamp = measure ? Stopwatch.GetTimestamp() : 0;

        if (panAnchor is not null)
        {
            camera.Move(panAnchor.Value - GetWorldPoint(e));
            redrawRequested = true;
        }

        Point worldPoint = GetWorldPoint(e);
        IReadOnlyList<WorldRenderContent.HitTarget> hitTargets = snapshotWorker.Current?.WorldContent.HitTargets ?? [];
        WorldPosition? hitPosition = WorldHitTester.HitTest(hitTargets, worldPoint);
        ViewModel.UpdateHover(WorldTransform.FromWorldPoint(worldPoint), hitPosition);

        if (measure)
        {
            Diagnostics.AddPointerMove(Stopwatch.GetElapsedTime(moveStartTimestamp), hitTargets.Count);
        }
    }

    private Point GetWorldPoint(PointerEventArgs e)
    {
        return camera.ScreenToWorld(e.GetPosition(this));
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        ViewModel.ClearHover();
    }

    private void OnTopLevelPointerMoved(object? sender, PointerEventArgs e)
    {
        Point position = e.GetPosition(this);
        Rect interfaceRect = new Rect(Bounds.Size).Deflate(InterfaceScrollMargin);

        if (e.Source != this && interfaceRect.Contains(position))
        {
            scrollHelper.HandlePointerExited();
        }
        else
        {
            scrollHelper.HandlePointerMoved(position);
        }
    }

    private void OnTopLevelPointerExited(object? sender, PointerEventArgs e)
    {
        scrollHelper.HandlePointerExited();
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

        long startTimestamp = measure ? Stopwatch.GetTimestamp() : 0;

        Exception? workerException = snapshotWorker.Exception;
        if (workerException is not null)
        {
            StopFrames();
            ViewModel.HandleRenderException(workerException);
            return;
        }

        bool didRedraw = false;

        try
        {
            if (snapshotWorker.Current is not null)
            {
                ViewModel.CompleteGameStart();
            }

            TimeSpan delta = animationClock.Tick();

            double playbackRate = ViewModel.IsFastForward ? FastForwardRate : 1.0;
            playbackClock.Advance(delta * playbackRate);

            TimeSpan now = playbackClock.Now;

            snapshotWorker.Request(ViewModel.RenderRequest);
            RegisterAnimation(snapshotWorker.Current, now);
            ViewModel.Update(delta, animationPlayback.GetElapsed(now));

            if (animationPlayback.TryComplete(now))
            {
                ViewModel.CompleteAnimation();
            }

            int frameIndex = AnimationRate.Sprites.GetFrameIndex(animationClock.Now);
            bool redraw = false;

            if (redrawRequested)
            {
                redraw = true;
            }

            if (!ReferenceEquals(snapshotWorker.Current, lastSnapshot))
            {
                redraw = true;
            }

            if (ViewModel.IsActive)
            {
                UpdateCameraBounds();

                if (UpdateCamera(delta))
                {
                    redraw = true;
                }
            }

            if (animationPlayback.IsPlaying)
            {
                redraw = true;
            }

            if (ViewModel.IsActive || ViewModel.Game.TopScreen is AbstractLiveBackgroundScreenViewModel)
            {
                if (frameIndex != lastFrameIndex)
                {
                    redraw = true;
                }

                ambientSoundController.Update(snapshotWorker.Current?.WorldState.AmbientSounds ?? [], camera.Center, Audio);
            }
            else
            {
                ambientSoundController.Stop();
            }

            if (redraw)
            {
                didRedraw = true;
                redrawRequested = false;
                lastFrameIndex = frameIndex;
                lastSnapshot = snapshotWorker.Current;
                InvalidateVisual();
            }
        }
        finally
        {
            if (measure)
            {
                if (elapsedTime > TimeSpan.Zero)
                {
                    Diagnostics.AddFrame(elapsedTime, didRedraw && lastFrameDidRedraw);
                }

                Diagnostics.AddLoop(Stopwatch.GetElapsedTime(startTimestamp));
            }

            lastFrameDidRedraw = didRedraw;
        }
    }

    private void UpdateCameraBounds()
    {
        camera.ViewportSize = Bounds.Size;
        camera.MapWidth = ViewModel.MapWidth;
        camera.MapHeight = ViewModel.MapHeight;
    }

    private bool UpdateCamera(TimeSpan delta)
    {
        Vector direction = GetKeyboardPanDirection();

        if (direction is { X: 0, Y: 0 })
        {
            direction = scrollHelper.GetDirection(Bounds.Size);
        }

        bool moved = false;

        if (direction is not { X: 0, Y: 0 })
        {
            Vector normalized = direction.Normalize();
            moved = camera.PanBy(normalized * (CameraSpeed * delta.TotalSeconds / camera.Zoom));
        }

        return camera.Animate(delta) || moved;
    }

    private Vector GetKeyboardPanDirection()
    {
        double directionX = 0;
        double directionY = 0;

        if (pressedKeys.Contains(Key.Left) || pressedKeys.Contains(Key.A))
        {
            directionX -= 1;
        }

        if (pressedKeys.Contains(Key.Right) || pressedKeys.Contains(Key.D))
        {
            directionX += 1;
        }

        if (pressedKeys.Contains(Key.Up) || pressedKeys.Contains(Key.W))
        {
            directionY -= 1;
        }

        if (pressedKeys.Contains(Key.Down) || pressedKeys.Contains(Key.S))
        {
            directionY += 1;
        }

        return new Vector(directionX, directionY);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        WorldScreenSnapshot? snapshot = snapshotWorker.Current;

        if (snapshot is null)
        {
            context.FillRectangle(BackdropBrush, new Rect(Bounds.Size));

            return;
        }

        TimeSpan loopTime = animationClock.Now;
        TimeSpan playbackTime = playbackClock.Now;

        RegisterAnimation(snapshot, playbackTime);

        TimeSpan animationTime = animationPlayback.GetElapsed(playbackTime);
        Matrix view = camera.GetViewMatrix();
        GameSettings settings = SettingsScope.GetSettings(this);

        context.Custom(new WorldRenderOperation(
            new Rect(Bounds.Size),
            view,
            snapshot.WorldState,
            snapshot.WorldContent,
            renderer,
            Assets,
            loopTime,
            animationTime,
            settings.RenderResolutionRatio,
            IsHitboxOverlayVisible,
            Diagnostics));
    }

    private void RegisterAnimation(WorldScreenSnapshot? snapshot, TimeSpan now)
    {
        WorldRenderState.WorldAnimation? animation = snapshot?.WorldState.Animation;

        if (!animationPlayback.Register(animation, now) || animation is null)
        {
            return;
        }

        if (animation.Accept(WorldAnimationSoundVisitor.Instance, Empty.Value) is { } soundKey)
        {
            Audio.PlaySound(soundKey);
        }
    }
}
