namespace Packer.Tests;

internal sealed class TestDownloadProgress : IProgress<DownloadProgress>
{
    public List<DownloadProgress> Reports { get; } = [];

    public void Report(DownloadProgress value)
    {
        Reports.Add(value);
    }
}
