using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Styling;
using AvaloniaAnimation = Avalonia.Animation.Animation;

namespace Gallean.Client;

internal sealed class CrossFadePageTransition : IPageTransition
{
    public TimeSpan Duration { get; set; }

    public Easing Easing { get; set; } = new SineEaseInOut();

    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (from is not null)
        {
            from.ZIndex = 0;
            from.Opacity = 1;
        }

        if (to is not null)
        {
            to.ZIndex = 1;
            to.Opacity = 0;
            to.IsVisible = true;

            AvaloniaAnimation animation = new()
            {
                Duration = Duration,
                Easing = Easing,
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame { Cue = new Cue(0), Setters = { new Setter(Visual.OpacityProperty, 0.0) } },
                    new KeyFrame { Cue = new Cue(1), Setters = { new Setter(Visual.OpacityProperty, 1.0) } }
                }
            };

            await animation.RunAsync(to, cancellationToken);

            to.Opacity = 1;
        }

        if (from is not null && !cancellationToken.IsCancellationRequested)
        {
            from.IsVisible = false;
        }
    }
}
