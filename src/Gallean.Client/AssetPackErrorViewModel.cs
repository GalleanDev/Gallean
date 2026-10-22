using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed partial class AssetPackErrorViewModel(
    InstalledAssetPackException exception,
    string gameDirectory,
    IControlledApplicationLifetime lifetime)
    : ObservableObject
{
    public string Title { get; } = FormatTitle(exception);
    public string Message { get; } = FormatMessage(exception);
    public string Location { get; } = gameDirectory;
    public Uri GetPackUri { get; } = new("https://gallean.org/go/1");

    [RelayCommand]
    private void Exit()
    {
        lifetime.Shutdown();
    }

    private static string FormatTitle(InstalledAssetPackException exception)
    {
        return exception switch
        {
            InstalledAssetPackNotFoundException => Tr("Asset pack is required"),
            InstalledAssetPackIncompatibleException => Tr("Incompatible asset pack"),
            InstalledAssetPackNotEnhancedException => Tr("Unexpected asset pack"),
            _ => throw new ArgumentOutOfRangeException(nameof(exception), exception, null)
        };
    }

    private static string FormatMessage(InstalledAssetPackException exception)
    {
        return exception switch
        {
            InstalledAssetPackNotFoundException =>
                Tr("Gallean is an open-source implementation of Disciples II game mechanics. To play, you need an asset pack built from the original game assets."),
            InstalledAssetPackIncompatibleException =>
                Tr("The installed asset pack was made for a different version of Gallean. Replace it with a pack made for this version."),
            InstalledAssetPackNotEnhancedException =>
                Tr("The installed asset pack does not include the enhanced assets. Replace it with an appropriate pack."),
            _ => throw new ArgumentOutOfRangeException(nameof(exception), exception, null)
        };
    }
}
