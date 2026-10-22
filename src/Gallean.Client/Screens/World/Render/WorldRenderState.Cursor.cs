using Avalonia.Media;

namespace Gallean.Client;

internal sealed partial record WorldRenderState
{
    public sealed record Cursor(WorldPosition Position, int Depth, Color InnerColor, Color OuterColor, double Scale);
}
