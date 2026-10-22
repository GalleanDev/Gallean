using CommunityToolkit.Mvvm.ComponentModel;

namespace Gallean.Client;

internal sealed partial class DropZoneViewModel : ObservableObject, IDropTarget
{
    [ObservableProperty]
    public partial object? Content { get; private set; }

    [ObservableProperty]
    public partial bool IsVisible { get; private set; }

    public IDragTargetReference DropReference => new DropZoneReference(this);

    public void Show(object content)
    {
        Content = content;
        IsVisible = true;
    }

    public void Hide()
    {
        Content = null;
        IsVisible = false;
    }
}
