using System.Collections.ObjectModel;

namespace Gallean.Client;

internal sealed partial class EventLogPopupViewModel(ObservableCollection<EventLogEntry> entries) : AbstractScreenViewModel
{
    public ReadOnlyObservableCollection<EventLogEntry> Entries { get; } = new(entries);

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }
}
