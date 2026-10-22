using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Styling;
using AvaloniaAnimation = Avalonia.Animation.Animation;

namespace Gallean.Client;

internal sealed class AnimationPropertyPlayer
{
    private sealed class EasingAdapter(AnimationEasing easing) : Easing
    {
        public override double Ease(double progress)
        {
            return easing.Ease(progress);
        }
    }

    private CancellationTokenSource? cts;

    public static AvaloniaAnimation Compose(AnimationProperty property, AvaloniaProperty target)
    {
        AvaloniaAnimation animation = new()
        {
            Delay = property.Start,
            Duration = property.Duration,
            FillMode = FillMode.Forward,
            Easing = new EasingAdapter(property.Easing)
        };

        animation.Children.Add(BuildKeyFrame(0, target, property.From));
        animation.Children.Add(BuildKeyFrame(1, target, property.To));

        return animation;
    }

    public void Start(Animatable target, AvaloniaProperty property, IReadOnlyList<AnimationProperty> properties)
    {
        Stop();

        if (properties.Count > 0)
        {
            cts = new CancellationTokenSource();

            foreach (AnimationProperty animated in properties)
            {
                Task task = RunAsync(Compose(animated, property), target, cts.Token);

                task.FileAndForget();
            }
        }
    }

    public void Stop()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    private static async Task RunAsync(AvaloniaAnimation animation, Animatable target, CancellationToken token)
    {
        try
        {
            await animation.RunAsync(target, token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static KeyFrame BuildKeyFrame(double cue, AvaloniaProperty target, double value)
    {
        return new KeyFrame
        {
            Cue = new Cue(cue),
            Setters = { new Setter(target, value) }
        };
    }
}
