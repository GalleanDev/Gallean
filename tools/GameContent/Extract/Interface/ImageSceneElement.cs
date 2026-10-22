namespace GameContent;

public sealed record ImageSceneElement : SceneElement
{
    public override SceneElementType Type => SceneElementType.Image;

    public string? ImageName { get; init; }
    public string? ToolTipTextId { get; init; }
}
