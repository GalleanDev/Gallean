using Avalonia;
using Avalonia.Animation;
using Avalonia.Styling;
using Gallean.Tests.Common;
using AvaloniaAnimation = Avalonia.Animation.Animation;

namespace Gallean.Client.Tests;

public sealed class AnimationPropertyPlayerTests
{
    private sealed record TestTarget(string Name) : AnimationTarget;

    private static IReadOnlyList<string> Format(AvaloniaAnimation animation)
    {
        List<string> rows =
        [
            $"delay {animation.Delay.TotalMilliseconds} duration {animation.Duration.TotalMilliseconds} {animation.Easing.Ease(0.25)} {animation.FillMode}"
        ];

        foreach (KeyFrame frame in animation.Children)
        {
            foreach (IAnimationSetter entry in frame.Setters)
            {
                Setter setter = Assert.IsOfType<Setter>(entry);

                rows.Add($"@{frame.Cue.CueValue} {setter.Property?.Name} {setter.Value}");
            }
        }

        return rows;
    }

    [Test]
    public void ComposeTest()
    {
        AnimationProperty property = new(
            new TestTarget("wound"),
            TimeSpan.FromSeconds(0.25),
            TimeSpan.FromSeconds(0.5),
            From: 1,
            To: 0.25,
            AnimationEasing.Smooth);

        AvaloniaAnimation animation = AnimationPropertyPlayer.Compose(property, Visual.OpacityProperty);

        Assert.That(Format(animation), Is.EqualTo([
            "delay 250 duration 500 0.15625 Forward",
            "@0 Opacity 1",
            "@1 Opacity 0.25"
        ]));
    }

    [Test]
    public void LinearEasingTest()
    {
        AnimationProperty property = new(
            new TestTarget("wound"),
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            From: 0,
            To: 1,
            AnimationEasing.Linear);

        AvaloniaAnimation animation = AnimationPropertyPlayer.Compose(property, Visual.OpacityProperty);

        Assert.That(Format(animation), Is.EqualTo([
            "delay 0 duration 1000 0.25 Forward",
            "@0 Opacity 0",
            "@1 Opacity 1"
        ]));
    }
}
