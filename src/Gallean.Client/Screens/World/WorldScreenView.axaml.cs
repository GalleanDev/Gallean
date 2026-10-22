using System.ComponentModel;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Gallean.Common;
using Gallean.Engine;
using AvaloniaAnimation = Avalonia.Animation.Animation;

namespace Gallean.Client;

internal sealed partial class WorldScreenView : UserControl
{
    private readonly IGameDefinitionProvider definitions;
    private readonly IGameScenarioProvider scenario;
    private readonly DiagnosticsRecorder diagnostics;
    private CleanupList? cleanupList;

    public WorldScreenView(IGameDefinitionProvider definitions, IGameScenarioProvider scenario, DiagnosticsRecorder diagnostics)
    {
        this.definitions = definitions;
        this.scenario = scenario;
        this.diagnostics = diagnostics;

        InitializeComponent();

        World.AddHandler(PointerPressedEvent, OnWorldPointerPressed, RoutingStrategies.Tunnel);
    }

    internal WorldScreenSnapshotWorker? SnapshotWorker { get; private set; }
    internal WorldCamera Camera { get; } = new();

    private WorldScreenViewModel ViewModel => (WorldScreenViewModel)DataContext!;
    private IAssetProvider Assets => AssetScope.GetRequiredAssets(this);

    private void OnWorldPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PointerPoint point = e.GetCurrentPoint(World);

        if (point.Properties.IsRightButtonPressed)
        {
            Rect cursor = new(point.Position, new Size(1, 1));
            PositionInfoPopup.PlacementRect = cursor.Inflate(new Thickness(24, 0));
        }
    }

    private void OnStackCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!ViewModel.IsActive)
        {
            return;
        }

        if (sender is Button { DataContext: WorldStackItemViewModel item } card)
        {
            PointerPoint point = e.GetCurrentPoint(card);

            if (point.Properties.IsRightButtonPressed)
            {
                Matrix? transform = card.TransformToVisual(World);

                if (transform is not null)
                {
                    Rect bounds = new(card.Bounds.Size);
                    PositionInfoPopup.PlacementRect = bounds.TransformToAABB(transform.Value);
                }

                ViewModel.ShowStackInfo(item.StackId);
                e.Handled = true;
            }
        }
    }

    private void OnStackCardPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            ViewModel.HidePositionInfo();
            e.Handled = true;
        }
    }

    private void OnStackCardDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Button { DataContext: WorldStackItemViewModel { IsSelected: true } })
        {
            if (ViewModel.InspectStackCommand.CanExecute(null))
            {
                ViewModel.InspectStackCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private void OnStackCardPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        ViewModel.HidePositionInfo();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        SnapshotWorker = new WorldScreenSnapshotWorker(Assets, definitions, scenario, diagnostics);

        World.Attach(Camera, SnapshotWorker);
        Minimap.Attach(Camera, SnapshotWorker);

        CleanupList cleanup = new();
        cleanupList = cleanup;

        WorldScreenViewModel viewModel = ViewModel;
        viewModel.ObjectiveChanged += OnObjectiveChanged;
        cleanup.Add(new ActionDisposable(() => viewModel.ObjectiveChanged -= OnObjectiveChanged));
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        cleanup.Add(new ActionDisposable(() => viewModel.PropertyChanged -= OnViewModelPropertyChanged));
        Camera.Changed += UpdateActionWheelAnchor;
        cleanup.Add(new ActionDisposable(() => Camera.Changed -= UpdateActionWheelAnchor));

        UpdateActionWheelAnchor();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        SnapshotWorker?.Dispose();
        SnapshotWorker = null;

        cleanupList?.Dispose();
        cleanupList = null;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(WorldScreenViewModel.ActionWheel))
        {
            UpdateActionWheelAnchor();
        }
    }

    private void UpdateActionWheelAnchor()
    {
        WorldActionWheelViewModel? wheel = ViewModel.ActionWheel;

        if (wheel is not null)
        {
            Point point = WorldTransform.ToWorldPoint(wheel.Position).Transform(Camera.GetViewMatrix());

            ActionWheelAnchor.Anchor = new RelativePoint(point, RelativeUnit.Absolute);
        }
    }

    private void OnObjectiveChanged()
    {
        AvaloniaAnimation animation = new()
        {
            Duration = TimeSpan.FromSeconds(0.7),
            IterationCount = new IterationCount(3),
            Easing = new SineEaseInOut(),
            Children =
            {
                MakeKeyFrame(0.0, 0.0),
                MakeKeyFrame(0.5, 1.0),
                MakeKeyFrame(1.0, 0.0)
            }
        };

        animation.RunAsync(ObjectiveHighlight).FileAndForget();

        static KeyFrame MakeKeyFrame(double cue, double opacity)
        {
            return new KeyFrame
            {
                Cue = new Cue(cue),
                Setters = { new Setter(OpacityProperty, opacity) }
            };
        }
    }
}
