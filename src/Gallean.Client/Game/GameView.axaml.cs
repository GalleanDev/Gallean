using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Gallean.Client;

internal sealed partial class GameView : Window
{
    public static readonly StyledProperty<bool> IsWindowedProperty =
        AvaloniaProperty.Register<GameView, bool>(nameof(IsWindowed), defaultValue: true, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<AbstractScreenViewModel?> TopScreenProperty =
        AvaloniaProperty.Register<GameView, AbstractScreenViewModel?>(nameof(TopScreen));

    private readonly Size minSize;
    private Task? exitSaveTask;

    public GameView()
    {
        InitializeComponent();

        minSize = new Size(MinWidth, MinHeight);

        ScalingChanged += OnScalingChanged;
        PositionChanged += OnPositionChanged;
        Resized += OnResized;

        AddHandler(PointerPressedEvent, OnDetailsPointerPressed, RoutingStrategies.Tunnel, true);
        AddHandler(PointerReleasedEvent, OnDetailsPointerReleased, RoutingStrategies.Tunnel, true);
        AddHandler(PointerCaptureLostEvent, OnDetailsPointerCaptureLost, RoutingStrategies.Tunnel, true);
    }

    private GameViewModel? ViewModel => (GameViewModel?)DataContext;

    public bool IsWindowed
    {
        get => GetValue(IsWindowedProperty);
        set => SetValue(IsWindowedProperty, value);
    }

    public AbstractScreenViewModel? TopScreen
    {
        get => GetValue(TopScreenProperty);
        set => SetValue(TopScreenProperty, value);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        FitToScreen();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        GameViewModel? game = ViewModel;

        if (e.IsProgrammatic || game is null)
        {
            return;
        }

        e.Cancel = true;

        if (exitSaveTask is null)
        {
            exitSaveTask = SaveAndCloseAsync(game);
            exitSaveTask.FileAndForget();
        }
    }

    private async Task SaveAndCloseAsync(GameViewModel game)
    {
        await game.WriteExitSaveAsync();

        Close();
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);

        if (e.Source is Visual focused)
        {
            focused.DetachedFromVisualTree += OnFocusedDetached;
        }
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);

        if (e.Source is Visual focused)
        {
            focused.DetachedFromVisualTree -= OnFocusedDetached;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!e.Handled && TopScreen is not null)
        {
            Control? container = ScreenHost.ContainerFromItem(TopScreen);

            if (container is ContentPresenter { Child: not null } presenter)
            {
                foreach (KeyBinding binding in presenter.Child.KeyBindings)
                {
                    if (e.Handled)
                    {
                        break;
                    }

                    binding.TryHandle(e);
                }
            }
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == IsWindowedProperty)
        {
            bool windowed = WindowState != WindowState.FullScreen;

            if (IsWindowed != windowed)
            {
                WindowState = IsWindowed ? WindowState.Normal : WindowState.FullScreen;
            }
        }
        else if (e.Property == TopScreenProperty)
        {
            FocusTopScreen();
        }
        else if (e.Property == WindowStateProperty)
        {
            bool windowed = WindowState != WindowState.FullScreen;

            if (WindowState != WindowState.Minimized)
            {
                SetCurrentValue(IsWindowedProperty, windowed);
            }

            FitToScreen();
        }
    }

    private void OnFocusedDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Visual visual)
        {
            visual.DetachedFromVisualTree -= OnFocusedDetached;
        }

        FocusTopScreen();
    }

    private void OnScalingChanged(object? sender, EventArgs e)
    {
        FitToScreen();
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        FitToScreen();
    }

    private void OnResized(object? sender, WindowResizedEventArgs e)
    {
        FitToScreen();
    }

    private void FitToScreen()
    {
        Screen? screen = Screens.ScreenFromWindow(this);

        if (screen is null)
        {
            return;
        }

        PixelRect area = WindowState == WindowState.FullScreen ? screen.Bounds : screen.WorkingArea;
        PixelSize areaPixels = area.Size;
        Size areaSize = areaPixels.ToSize(DesktopScaling);
        Size frameSize = FrameSize ?? ClientSize;
        Size decorationSize = frameSize - ClientSize;
        Size maxClientSize = areaSize - decorationSize;

        MinWidth = Math.Min(minSize.Width, maxClientSize.Width);
        MinHeight = Math.Min(minSize.Height, maxClientSize.Height);

        if (WindowState == WindowState.Normal)
        {
            Size requestedSize = new(Width, Height);

            if (requestedSize.Width > maxClientSize.Width || requestedSize.Height > maxClientSize.Height)
            {
                Size clientSize = requestedSize.Constrain(maxClientSize);
                PixelSize pixelSize = PixelSize.FromSize(clientSize + decorationSize, DesktopScaling);

                Width = clientSize.Width;
                Height = clientSize.Height;
                Position = new PixelPoint(
                    Math.Max(area.X, Math.Min(Position.X, area.Right - pixelSize.Width)),
                    Math.Max(area.Y, Math.Min(Position.Y, area.Bottom - pixelSize.Height)));
            }
        }
    }

    private void OnDetailsPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PointerPoint point = e.GetCurrentPoint(this);
        GameViewModel? game = ViewModel;

        if (point.Properties.IsRightButtonPressed && game is not null)
        {
            object? content = GetDetailsContentAt(e);

            if (content is not null)
            {
                game.ShowDetails(content);
                e.Pointer.Capture(this);
                e.Handled = true;
            }
        }
    }

    private void OnDetailsPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        GameViewModel? game = ViewModel;

        if (e.InitialPressMouseButton == MouseButton.Right && game is not null)
        {
            game.HideDetails();

            if (ReferenceEquals(e.Pointer.Captured, this))
            {
                e.Pointer.Capture(null);
            }
        }
    }

    private void OnDetailsPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        ViewModel?.HideDetails();
    }

    private object? GetDetailsContentAt(PointerEventArgs e)
    {
        if (TopScreen is not null)
        {
            Control? container = ScreenHost.ContainerFromItem(TopScreen);

            if (container is not null)
            {
                Point position = e.GetPosition(this);

                foreach (Visual visual in this.GetVisualsAt(position))
                {
                    IDetailsSource? source = FindDetailsSource(visual, container);

                    if (source is not null)
                    {
                        return source.DetailsContent;
                    }
                }
            }
        }

        return null;
    }

    private static IDetailsSource? FindDetailsSource(Visual visual, Control screen)
    {
        IDetailsSource? source = null;

        for (Visual? current = visual; current is not null; current = GetParent(current))
        {
            if (current == screen)
            {
                return source;
            }

            source ??= current.DataContext as IDetailsSource;
        }

        return null;

        static Visual? GetParent(Visual visual)
        {
            if (visual is OverlayPopupHost)
            {
                return visual.Parent as Visual;
            }
            else
            {
                return visual.GetVisualParent();
            }
        }
    }

    private void FocusTopScreen()
    {
        if (TopScreen is not null)
        {
            Control? container = ScreenHost.ContainerFromItem(TopScreen);

            if (container is ContentPresenter presenter)
            {
                presenter.ApplyTemplate();
                presenter.Child?.Focus();
            }
        }
    }
}
