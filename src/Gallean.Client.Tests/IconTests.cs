using Avalonia;
using Avalonia.Headless.NUnit;

namespace Gallean.Client.Tests;

[NonParallelizable]
public sealed class IconTests
{
    [AvaloniaTest]
    public void ResolveTest()
    {
        foreach (IconMoniker moniker in Enum.GetValues<IconMoniker>())
        {
            IconGeometry icon = Icons.Get(moniker);

            Assert.That(icon.ViewBox, Is.GreaterThan(0), $"{moniker} view box");
            Assert.That(icon.Layers, Is.Not.Empty, $"{moniker} layers");

            foreach (IconLayer layer in icon.Layers)
            {
                Assert.That(layer.Geometry.Bounds.Width, Is.GreaterThan(0), $"{moniker} width");
                Assert.That(layer.Geometry.Bounds.Height, Is.GreaterThan(0), $"{moniker} height");
            }
        }
    }

    [AvaloniaTest]
    public void ViewBoxTest()
    {
        foreach (IconMoniker moniker in Enum.GetValues<IconMoniker>())
        {
            IconGeometry icon = Icons.Get(moniker);

            foreach (IconLayer layer in icon.Layers)
            {
                Rect bounds = layer.Geometry.Bounds;

                Assert.That(bounds.Left, Is.GreaterThanOrEqualTo(-0.5), $"{moniker} left");
                Assert.That(bounds.Top, Is.GreaterThanOrEqualTo(-0.5), $"{moniker} top");
                Assert.That(bounds.Right, Is.LessThanOrEqualTo(icon.ViewBox + 0.5), $"{moniker} right");
                Assert.That(bounds.Bottom, Is.LessThanOrEqualTo(icon.ViewBox + 0.5), $"{moniker} bottom");
            }
        }
    }
}
