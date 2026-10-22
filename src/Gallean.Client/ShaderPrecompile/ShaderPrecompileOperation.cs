using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;

namespace Gallean.Client;

internal sealed class ShaderPrecompileOperation(Rect bounds, Action<ImmediateDrawingContext> precompile) : ICustomDrawOperation
{
    private bool precompiled;

    public Rect Bounds => bounds;

    public bool HitTest(Point point)
    {
        return false;
    }

    public bool Equals(ICustomDrawOperation? other)
    {
        return ReferenceEquals(this, other);
    }

    public void Dispose()
    {
    }

    public void Render(ImmediateDrawingContext context)
    {
        if (!precompiled)
        {
            precompiled = true;
            precompile(context);
        }
    }
}
