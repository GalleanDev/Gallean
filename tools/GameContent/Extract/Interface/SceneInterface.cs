namespace GameContent;

public sealed record SceneInterface
{
    public required string Name { get; init; }
    public Rectangle Bounds { get; init; }
    public string? BackgroundImageName { get; init; }
    public string? CursorImageName { get; init; }
    public Point CursorHotSpot { get; init; }
    public Rectangle Position { get; init; }
    public bool IsSelfDrawn { get; init; }
    public IReadOnlyList<SceneElement> Elements { get; init; } = [];
}
