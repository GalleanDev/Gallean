using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaAnimation = Avalonia.Animation.Animation;

namespace Gallean.Client;

internal sealed class EndTurnIconDecorator : Decorator
{
    public static readonly StyledProperty<bool> IsSpinningProperty =
        AvaloniaProperty.Register<EndTurnIconDecorator, bool>(nameof(IsSpinning));

    private readonly RotateTransform rotation = new();
    private CancellationTokenSource? cts;

    public EndTurnIconDecorator()
    {
        RenderTransformOrigin = RelativePoint.Center;
        RenderTransform = rotation;
        Child = new IconVisual { Moniker = IconMoniker.EndTurn };
    }

    public bool IsSpinning
    {
        get => GetValue(IsSpinningProperty);
        set => SetValue(IsSpinningProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == IsSpinningProperty && IsLoaded)
        {
            Start();
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        Start();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        cts?.Cancel();
        cts?.Dispose();
        cts = null;
        rotation.Angle = 0;
    }

    private void Start()
    {
        if (IsSpinning && cts is null)
        {
            cts = new CancellationTokenSource();
            SpinAsync(cts).FileAndForget();
        }
    }

    private async Task SpinAsync(CancellationTokenSource source)
    {
        AvaloniaAnimation spin = new()
        {
            Duration = TimeSpan.FromSeconds(1.6),
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(RotateTransform.AngleProperty, 0.0) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(RotateTransform.AngleProperty, 360.0) } }
            }
        };

        try
        {
            do
            {
                await spin.RunAsync(this, source.Token);
            } while (IsSpinning && !source.IsCancellationRequested);
        }
        catch (OperationCanceledException)
        {
        }

        if (cts == source)
        {
            cts.Dispose();
            cts = null;
        }
    }
}
