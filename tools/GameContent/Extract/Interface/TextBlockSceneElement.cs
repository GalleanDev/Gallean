namespace GameContent;

public sealed record TextBlockSceneElement : SceneElement
{
    public override SceneElementType Type => SceneElementType.TextBlock;

    public string? TextStyle { get; init; }
    public string? TextId { get; init; }
    public string? ToolTipTextId { get; init; }
}
