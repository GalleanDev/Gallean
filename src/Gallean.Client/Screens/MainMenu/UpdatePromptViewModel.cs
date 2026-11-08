namespace Gallean.Client;

internal sealed class UpdatePromptViewModel(UpdateInfo update, ClientVersion currentVersion) : AbstractScreenViewModel
{
    public TextSpans Message { get; } = TrSpans("New version {0:*} is available. You are playing version {1:*}.", update.Version.Version, currentVersion.Version);
    public Uri ReleaseUri { get; } = update.ReleaseUri;

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }
}
