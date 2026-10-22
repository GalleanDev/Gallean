namespace Gallean.Client;

internal interface IDragSource
{
    DragPreview? DragPreview { get; }
    IDragSourceReference DragReference { get; }
}
