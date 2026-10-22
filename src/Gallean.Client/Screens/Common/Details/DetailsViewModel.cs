namespace Gallean.Client;

internal sealed class DetailsViewModel(object content) : AbstractScreenViewModel
{
    public object Content { get; } = content;

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }
}
