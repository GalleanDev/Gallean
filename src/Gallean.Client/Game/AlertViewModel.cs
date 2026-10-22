using CommunityToolkit.Mvvm.ComponentModel;

namespace Gallean.Client;

internal sealed class AlertViewModel : ObservableObject
{
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(2.5);

    public Alert? Current
    {
        get;
        private set => SetProperty(ref field, value, ReferenceEqualityComparer.Instance);
    }

    public void Show(Alert alert)
    {
        Current = alert;
        HideAsync(alert).FileAndForget();
    }

    public void Dismiss()
    {
        Current = null;
    }

    private async Task HideAsync(Alert alert)
    {
        await Task.Delay(Duration);

        if (ReferenceEquals(Current, alert))
        {
            Current = null;
        }
    }
}
