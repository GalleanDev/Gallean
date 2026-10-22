using CommunityToolkit.Mvvm.ComponentModel;

namespace Gallean.Client;

internal sealed partial class DragDropViewModel(GameModel gameModel, IDragDropProvider provider) : ObservableObject
{
    [ObservableProperty]
    public partial IDragSourceReference? Dragged { get; private set; }

    public bool TryBeginDrag(IDragSourceReference source)
    {
        if (gameModel.IsBusy)
        {
            return false;
        }

        Dragged = source;
        provider.OnDragChanged();

        return true;
    }

    public void EndDrag()
    {
        if (Dragged is null)
        {
            return;
        }

        Dragged = null;
        provider.OnDragChanged();
    }

    public async Task DropAsync(IDragTargetReference target)
    {
        IDragSourceReference? source = Dragged;

        EndDrag();

        if (source is not null && !gameModel.IsBusy)
        {
            await provider.DropAsync(source, target);
        }
    }
}
