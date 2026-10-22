using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Gallean.Client;

internal abstract class AbstractShaderPrecompileVisual : Control
{
    protected const double Extent = 4.0;

    private ShaderPrecompileOperation? operation;

    protected AbstractShaderPrecompileVisual()
    {
        Width = Extent;
        Height = Extent;
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Top;
        ClipToBounds = true;
        IsHitTestVisible = false;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (operation is null)
        {
            operation = new ShaderPrecompileOperation(new Rect(Bounds.Size), BuildPrecompile());
        }

        context.Custom(operation);
    }

    protected abstract Action<ImmediateDrawingContext> BuildPrecompile();
}
