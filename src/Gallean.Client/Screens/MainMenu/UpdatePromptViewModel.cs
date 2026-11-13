using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed partial class UpdatePromptViewModel(
    UpdateInfo update,
    ClientVersion currentVersion,
    UpdateClient updateClient,
    UpdateInstaller updateInstaller,
    Action restartAction,
    ILog log) : AbstractScreenViewModel
{
    private readonly CancellationTokenSource cts = new();

    [ObservableProperty]
    public partial TextSpans Message { get; private set; } = TrSpans("New version {0:*} is available. You are playing version {1:*}.", update.Version.Version, currentVersion.Version);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CloseText))]
    public partial bool IsInstalling { get; private set; }

    [ObservableProperty]
    public partial bool CanClose { get; private set; } = true;

    [ObservableProperty]
    public partial double DownloadProgress { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    public partial bool CanInstall { get; private set; } = updateInstaller.CanInstall;

    public Uri ReleaseUri { get; } = update.ReleaseUri;

    public string CloseText
    {
        get
        {
            if (IsInstalling)
            {
                return Tr("Cancel");
            }
            else
            {
                return Tr("Later");
            }
        }
    }

    public override void Dispose()
    {
        cts.Cancel();
        cts.Dispose();

        base.Dispose();
    }

    protected override bool TryClose()
    {
        if (CanClose)
        {
            CloseSelf();
        }

        return CanClose;
    }

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private async Task InstallAsync()
    {
        CancellationToken cancellationToken = cts.Token;
        Progress<double> progress = new(OnDownloadProgress);

        IsInstalling = true;
        OnDownloadProgress(0);

        try
        {
            string packagePath = await updateClient.DownloadPackageAsync(update, progress, cancellationToken);

            CanClose = false;
            Message = TrSpans("Installing version {0}…", update.Version.Version);

            await Task.Run(() => updateInstaller.Install(packagePath), cancellationToken);
            File.Delete(packagePath);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            log.Error("Failed to install the update.", ex);

            IsInstalling = false;
            CanClose = true;
            CanInstall = false;
            Message = TrSpans("Version {0:*} could not be installed automatically. Download it from the website.", update.Version.Version);

            return;
        }

        restartAction();
    }

    private void OnDownloadProgress(double progress)
    {
        if (IsInstalling && CanClose)
        {
            DownloadProgress = progress;
            Message = TrSpans("Downloading version {0}: {1:*}", update.Version.Version, Tr("{0}%", (int)(progress * 100)));
        }
    }
}
