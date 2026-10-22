using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media;

namespace Gallean.Client.Tests;

[NonParallelizable]
public sealed class AnimatedFillTests
{
    private sealed record TestTarget(string Name) : AnimationTarget;

    [AvaloniaTest]
    public void ValueTest()
    {
        AnimatedFill fill = new() { Fill = Brushes.Red, Maximum = 30 };
        Window window = new() { Content = fill };
        window.Show();

        Assert.DoesNotThrow(() => fill.Value = 0);
        Assert.DoesNotThrow(() => fill.Value = 15);
        Assert.DoesNotThrow(() => fill.Value = 30);
        Assert.DoesNotThrow(() => fill.Value = -2);
        Assert.DoesNotThrow(() => fill.Value = 70);
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
