namespace GameContent;

public sealed record ToggleButtonSceneElement : SceneElement
{
    public override SceneElementType Type => SceneElementType.ToggleButton;

    public string? ActiveStateImageName { get; init; }
    public string? HoverStateImageName { get; init; }
    public string? PressedStateImageName { get; init; }
    public string? DisabledStateImageName { get; init; }
    public string? CheckedActiveStateImageName { get; init; }
    public string? CheckedHoverStateImageName { get; init; }
    public string? CheckedPressedStateImageName { get; init; }
    public string? ToolTipTextId { get; init; }
    public IReadOnlyList<int> HotKeys { get; init; } = [];
}
