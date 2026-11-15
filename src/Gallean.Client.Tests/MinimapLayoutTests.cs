using Avalonia;

namespace Gallean.Client.Tests;

public sealed class MinimapLayoutTests
{
    [Test]
    public void SimpleProjectionTest()
    {
        MinimapLayout layout = MinimapLayout.Create(4, 4, new Rect(0, 0, 200, 100));

        Assert.That(layout.CellToScreen(0, 0), Is.EqualTo(new Point(100, 20)));
        Assert.That(layout.CellToScreen(3, 0), Is.EqualTo(new Point(160, 50)));
        Assert.That(layout.CellToScreen(0, 3), Is.EqualTo(new Point(40, 50)));
        Assert.That(layout.CellToScreen(3, 3), Is.EqualTo(new Point(100, 80)));
    }

    [Test]
    public void LargeMapProjectionTest()
    {
        MinimapLayout layout = MinimapLayout.Create(144, 144, new Rect(0, 0, 145, 72.5));

        Assert.That(layout.CellToScreen(0, 0), Is.EqualTo(new Point(72.5, 0.5)));
        Assert.That(layout.CellToScreen(143, 0), Is.EqualTo(new Point(144.0, 36.25)));
        Assert.That(layout.CellToScreen(0, 143), Is.EqualTo(new Point(1.0, 36.25)));
        Assert.That(layout.CellToScreen(143, 143), Is.EqualTo(new Point(72.5, 72.0)));
    }

    [Test]
    public void RoundTripTest()
    {
        MinimapLayout layout = MinimapLayout.Create(6, 4, new Rect(0, 0, 240, 120));

        WorldPosition[] cells =
        [
            new(0, 0), new(1, 0), new(2, 0),
            new(3, 0), new(4, 0), new(5, 0),
            new(0, 1), new(1, 1), new(2, 1),
            new(3, 1), new(4, 1), new(5, 1),
            new(0, 2), new(1, 2), new(2, 2),
            new(3, 2), new(4, 2), new(5, 2),
            new(0, 3), new(1, 3), new(2, 3),
            new(3, 3), new(4, 3), new(5, 3)
        ];

        Assert.That(cells.Select(cell => layout.ToCell(layout.CellToScreen(cell.X, cell.Y))), Is.EqualTo(cells));
    }

    [Test]
    public void ContainsTest()
    {
        MinimapLayout layout = MinimapLayout.Create(4, 4, new Rect(0, 0, 200, 100));

        Assert.That(layout.Contains(layout.CellToScreen(0, 0)), Is.True);
        Assert.That(layout.Contains(layout.CellToScreen(3, 3)), Is.True);
        Assert.That(layout.Contains(new Point(100, -100)), Is.False);
        Assert.That(layout.Contains(new Point(100, 220)), Is.False);
    }
}
