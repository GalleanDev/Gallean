using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;

namespace Gallean.Client.Tests;

[NonParallelizable]
public sealed class FittingStackPanelTests
{
    private static FittingStackPanel CreatePanel(int childCount)
    {
        FittingStackPanel panel = new() { Spacing = 2.0, Overflow = 8.0 };

        for (int index = 0; index < childCount; index++)
        {
            panel.Children.Add(new Border { Width = 20.0, Height = 20.0 });
        }

        return panel;
    }

    private static List<string> FormatChildBounds(FittingStackPanel panel)
    {
        List<string> rows = [];

        foreach (Control child in panel.Children)
        {
            rows.Add($"{child.Bounds.X},{child.Bounds.Y} {child.Bounds.Width}x{child.Bounds.Height}");
        }

        return rows;
    }

    [AvaloniaTest]
    public void FitTest()
    {
        FittingStackPanel panel = CreatePanel(5);

        panel.Measure(new Size(30.0, 67.0));
        panel.Arrange(new Rect(panel.DesiredSize));

        Assert.That(panel.DesiredSize, Is.EqualTo(new Size(20.0, 64.0)));
        Assert.That(FormatChildBounds(panel), Is.EqualTo(new List<string>
        {
            "0,0 20x20",
            "0,22 20x20",
            "0,44 20x20",
            "0,82 20x20",
            "0,104 20x20"
        }));
    }

    [AvaloniaTest]
    public void UnboundedTest()
    {
        FittingStackPanel panel = CreatePanel(3);

        panel.Measure(Size.Infinity);
        panel.Arrange(new Rect(panel.DesiredSize));

        Assert.That(panel.DesiredSize, Is.EqualTo(new Size(20.0, 64.0)));
        Assert.That(FormatChildBounds(panel), Is.EqualTo(new List<string>
        {
            "0,0 20x20",
            "0,22 20x20",
            "0,44 20x20"
        }));
    }
}
