namespace GameContent;

public sealed record ButtonSceneElement : SceneElement
{
    public override SceneElementType Type => SceneElementType.Button;

    public string? ActiveStateImageName { get; init; }
    public string? HoverStateImageName { get; init; }
    public string? PressedStateImageName { get; init; }
    public string? DisabledStateImageName { get; init; }
    public string? ToolTipTextId { get; init; }
    public bool IsRepeat { get; init; }
    public IReadOnlyList<int> HotKeys { get; init; } = [];
}
