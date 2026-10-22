using System.ComponentModel;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Gallean.Common;
using AvaloniaAnimation = Avalonia.Animation.Animation;

namespace Gallean.Client;

internal sealed partial class AlertView : UserControl
{
    private static readonly TimeSpan RevealDuration = TimeSpan.FromSeconds(0.12);
    private static readonly TimeSpan DropDuration = TimeSpan.FromSeconds(0.32);
    private static readonly TimeSpan FadeDuration = TimeSpan.FromSeconds(0.3);

    private CancellationTokenSource? transition;
    private IDisposable? currentSubscription;
    private TopLevel? topLevel;

    public AlertView()
    {
        InitializeComponent();
    }

    private AlertViewModel ViewModel => (AlertViewModel)DataContext!;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        AlertViewModel viewModel = ViewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        currentSubscription = new ActionDisposable(() => viewModel.PropertyChanged -= OnViewModelPropertyChanged);

        topLevel = TopLevel.GetTopLevel(this);
        topLevel?.AddHandler(PointerPressedEvent, OnTopLevelPointerPressed, RoutingStrategies.Tunnel, true);
        topLevel?.AddHandler(PointerWheelChangedEvent, OnTopLevelPointerWheelChanged, RoutingStrategies.Tunnel, true);

        Alert? alert = viewModel.Current;

        if (alert is not null)
        {
            Show(alert);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        StopTransition();

        currentSubscription?.Dispose();
        currentSubscription = null;

        topLevel?.RemoveHandler(PointerPressedEvent, OnTopLevelPointerPressed);
        topLevel?.RemoveHandler(PointerWheelChangedEvent, OnTopLevelPointerWheelChanged);
        topLevel = null;
    }

    private void OnTopLevelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ViewModel.Dismiss();
    }

    private void OnTopLevelPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        ViewModel.Dismiss();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AlertViewModel.Current))
        {
            Alert? alert = ViewModel.Current;

            if (alert is not null)
            {
                Show(alert);
            }
            else
            {
                Hide();
            }
        }
    }

    private void StopTransition()
    {
        transition?.Cancel();
        transition?.Dispose();
        transition = null;
    }

    private void Show(Alert alert)
    {
        StopTransition();
        transition = new CancellationTokenSource();

        Plate.DataContext = alert;

        AlertIcon.Classes.Replace([
            alert.Type switch
            {
                AlertType.Error => "error",
                AlertType.Info => "info"
            }
        ]);

        AvaloniaAnimation reveal = new()
        {
            Duration = RevealDuration,
            FillMode = FillMode.Forward,
            Children =
            {
                MakeKeyFrame(0.0, OpacityProperty, 0.0),
                MakeKeyFrame(1.0, OpacityProperty, 1.0)
            }
        };

        AvaloniaAnimation drop = new()
        {
            Duration = DropDuration,
            Easing = new CubicEaseOut(),
            Children =
            {
                MakeKeyFrame(0.0, TranslateTransform.YProperty, -12.0),
                MakeKeyFrame(1.0, TranslateTransform.YProperty, 0.0)
            }
        };

        reveal.RunAsync(Plate, transition.Token).FileAndForget();
        drop.RunAsync(Plate, transition.Token).FileAndForget();
    }

    private void Hide()
    {
        StopTransition();
        transition = new CancellationTokenSource();

        AvaloniaAnimation fade = new()
        {
            Duration = FadeDuration,
            Easing = new SineEaseIn(),
            FillMode = FillMode.Forward,
            Children =
            {
                MakeKeyFrame(0.0, OpacityProperty, 1.0),
                MakeKeyFrame(1.0, OpacityProperty, 0.0)
            }
        };

        fade.RunAsync(Plate, transition.Token).FileAndForget();
    }

    private static KeyFrame MakeKeyFrame(double cue, AvaloniaProperty property, double value)
    {
        return new KeyFrame
        {
            Cue = new Cue(cue),
            Setters = { new Setter(property, value) }
        };
    }
}
