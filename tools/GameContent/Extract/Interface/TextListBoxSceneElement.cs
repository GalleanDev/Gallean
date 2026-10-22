namespace GameContent;

public sealed record TextListBoxSceneElement : SceneElement
{
    public override SceneElementType Type => SceneElementType.TextListBox;

    public int ColumnCount { get; init; }
    public int HorizontalSpacing { get; init; }
    public int VerticalSpacing { get; init; }
    public string? ScrollUpButtonName { get; init; }
    public string? ScrollDownButtonName { get; init; }
    public string? ScrollLeftButtonName { get; init; }
    public string? ScrollRightButtonName { get; init; }
    public string? PageUpButtonName { get; init; }
    public string? PageDownButtonName { get; init; }
    public string? DoubleClickButtonName { get; init; }
    public string? SelectedTextStyle { get; init; }
    public string? CommonTextStyle { get; init; }
    public string? SelectionImageName { get; init; }
    public string? UnselectedImageName { get; init; }
    public int BorderSize { get; init; }
    public string? ToolTipTextId { get; init; }
    public bool ShouldCreateBackgroundImage { get; init; }
}
