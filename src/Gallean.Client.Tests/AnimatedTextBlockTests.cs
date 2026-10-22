using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Threading;

namespace Gallean.Client.Tests;

[NonParallelizable]
public sealed class AnimatedTextBlockTests
{
    private static readonly FontFamily Font = new("avares://Gallean.Client/Theme/Assets#Alegreya Sans");

    private static AnimatedTextReveal MakeReveal(AnimatedTextSegmentType type)
    {
        return new AnimatedTextReveal(
            type,
            Stagger: TimeSpan.FromMilliseconds(10.0),
            Jitter: TimeSpan.Zero,
            Duration: TimeSpan.FromMilliseconds(100.0),
            Scatter: 0.0,
            Rise: 0.0,
            Scale: 1.0,
            Spin: 0.0,
            Curl: 0.0,
            Blur: 0.0,
            Smoke: 0.0,
            SmokeLift: 0.0,
            SmokeSwirl: 0.0,
            GlowBlur: 0.0);
    }

    [AvaloniaTest]
    public void WordTest()
    {
        AnimatedTextBlock block = new()
        {
            Text = "Keep watch\n\nover our pass",
            FontFamily = Font,
            Reveal = MakeReveal(AnimatedTextSegmentType.Word),
            RevealDelay = TimeSpan.FromSeconds(1.0)
        };

        Window window = new() { Content = block };
        window.Show();

        Assert.That(block.RevealEnd, Is.EqualTo(TimeSpan.FromMilliseconds(1140.0)));
    }

    [AvaloniaTest]
    public void GlyphTest()
    {
        AnimatedTextBlock block = new()
        {
            Text = "Keep watch\n\nover our pass",
            FontFamily = Font,
            Reveal = MakeReveal(AnimatedTextSegmentType.Glyph),
            RevealDelay = TimeSpan.FromSeconds(1.0)
        };

        Window window = new() { Content = block };
        window.Show();

        Assert.That(block.RevealEnd, Is.EqualTo(TimeSpan.FromMilliseconds(1290.0)));
    }

    [AvaloniaTest]
    public void WrappedTest()
    {
        AnimatedTextBlock block = new()
        {
            Text = "Keep watch over our pass",
            FontFamily = Font,
            FontSize = 12.0,
            TextWrapping = TextWrapping.Wrap,
            Width = 60.0,
            Reveal = MakeReveal(AnimatedTextSegmentType.Word),
            RevealDelay = TimeSpan.Zero
        };

        Window window = new() { Content = block };
        window.Show();

        Assert.That(block.TextLayout.TextLines.Count, Is.EqualTo(3));
        Assert.That(block.RevealEnd, Is.EqualTo(TimeSpan.FromMilliseconds(140.0)));
    }

    [AvaloniaTest]
    public void DelayTest()
    {
        AnimatedTextBlock block = new()
        {
            Text = "Keep watch",
            FontFamily = Font,
            Reveal = MakeReveal(AnimatedTextSegmentType.Word),
            RevealDelay = TimeSpan.FromSeconds(1.0)
        };

        Window window = new() { Content = block };
        window.Show();

        block.RevealDelay = TimeSpan.FromSeconds(3.0);

        Assert.That(block.RevealEnd, Is.EqualTo(TimeSpan.FromMilliseconds(3110.0)));
    }

    [AvaloniaTest]
    public void TextTest()
    {
        AnimatedTextBlock block = new()
        {
            Text = "Keep watch",
            FontFamily = Font,
            Reveal = MakeReveal(AnimatedTextSegmentType.Glyph),
            RevealDelay = TimeSpan.Zero
        };

        Window window = new() { Content = block };
        window.Show();

        block.Text = "Hold";
        Dispatcher.UIThread.RunJobs();

        Assert.That(block.RevealEnd, Is.EqualTo(TimeSpan.FromMilliseconds(130.0)));
    }

    [AvaloniaTest]
    public void NoRevealTest()
    {
        AnimatedTextBlock block = new()
        {
            Text = "Keep watch",
            FontFamily = Font,
            RevealDelay = TimeSpan.FromSeconds(1.0)
        };

        Window window = new() { Content = block };
        window.Show();

        Assert.That(block.RevealEnd, Is.EqualTo(TimeSpan.FromMilliseconds(1000.0)));
    }

    [AvaloniaTest]
    public void RemoveWhileAnimatedTest()
    {
        AnimatedTextBlock block = new()
        {
            Text = "Keep watch",
            FontFamily = Font,
            Reveal = AnimatedTextReveal.SmokeGlyphs,
            GlowColor = Colors.Orange
        };

        Panel panel = new();
        Window window = new() { Content = panel };
        panel.Children.Add(block);
        window.Show();

        Assert.DoesNotThrow(() => panel.Children.Clear());
        Assert.DoesNotThrow(() => panel.Children.Add(block));
    }
}
