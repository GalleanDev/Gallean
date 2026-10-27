using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media;

namespace Gallean.Client.Tests;

[NonParallelizable]
public sealed class AnimatedFillTests
{
    private sealed record TestTarget(string Name) : AnimationTarget;

    [AvaloniaTest]
    public void CurrentTest()
    {
        AnimatedFill fill = new() { Fill = Brushes.Red, Maximum = 30 };
        Window window = new() { Content = fill };
        window.Show();

        Assert.DoesNotThrow(() => fill.Current = 0);
        Assert.DoesNotThrow(() => fill.Current = 15);
        Assert.DoesNotThrow(() => fill.Current = 30);
        Assert.DoesNotThrow(() => fill.Current = -2);
        Assert.DoesNotThrow(() => fill.Current = 70);
        Assert.DoesNotThrow(() => fill.Maximum = 0);
    }

    [AvaloniaTest]
    public void RemoveWhileAnimatedTest()
    {
        AnimatedFill fill = new()
        {
            Fill = Brushes.Red,
            Animation =
            [
                new AnimationProperty(
                    new TestTarget("wound"),
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(10),
                    From: 0,
                    To: 1,
                    AnimationEasing.Linear)
            ]
        };

        Panel panel = new();
        Window window = new() { Content = panel };
        panel.Children.Add(fill);
        window.Show();

        Assert.DoesNotThrow(() => panel.Children.Clear());
    }
}
