namespace Gallean.Client;

internal interface IGroupDragSource : IDragSource
{
    IDragSourceReference GroupDragReference { get; }
}
