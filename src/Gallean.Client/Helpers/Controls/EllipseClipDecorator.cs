using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed class EllipseClipDecorator : Decorator
{
    protected override Size ArrangeOverride(Size finalSize)
    {
        Size size = base.ArrangeOverride(finalSize);
        Clip = new EllipseGeometry(new Rect(size));

        return size;
    }
}
