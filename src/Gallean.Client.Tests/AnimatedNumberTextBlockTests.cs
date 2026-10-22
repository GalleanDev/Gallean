using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;

namespace Gallean.Client.Tests;

[NonParallelizable]
public sealed class AnimatedNumberTextBlockTests
{
    private sealed record TestTarget(string Name) : AnimationTarget;

    [AvaloniaTest]
    public void ValueTest()
    {
        AnimatedNumberTextBlock block = new();
        Window window = new() { Content = block };
        window.Show();

        Assert.That(block.Text, Is.EqualTo("0"));

        block.Value = 12.6;

        Assert.That(block.Text, Is.EqualTo("13"));
    }

    [AvaloniaTest]
    public void AnimationTest()
    {
        AnimatedNumberTextBlock block = new()
        {
            Value = 25,
            Animation =
            [
                new AnimationProperty(
                    new TestTarget("gold"),
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(10),
                    From: 5,
                    To: 25,
                    AnimationEasing.Linear)
            ]
        };

        Window window = new() { Content = block };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.That(block.Text, Is.EqualTo("5"));
    }

    [AvaloniaTest]
    public void RemoveWhileAnimatedTest()
    {
        AnimatedNumberTextBlock block = new()
        {
            Animation =
            [
                new AnimationProperty(
                    new TestTarget("gold"),
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(10),
                    From: 0,
                    To: 1,
                    AnimationEasing.Linear)
            ]
        };

        Panel panel = new();
        Window window = new() { Content = panel };
        panel.Children.Add(block);
        window.Show();

        Assert.DoesNotThrow(() => panel.Children.Clear());
    }
}
