namespace Gallean.Client;

internal interface IDragDropProvider
{
    Task DropAsync(IDragSourceReference source, IDragTargetReference target);

    void OnDragChanged();
}
