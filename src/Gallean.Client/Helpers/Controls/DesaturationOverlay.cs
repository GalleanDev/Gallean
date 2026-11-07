using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed class DesaturationOverlay : Control
{
    public override void Render(DrawingContext context)
    {
        context.Custom(new DesaturationRenderOperation(new Rect(Bounds.Size)));
    }
}
