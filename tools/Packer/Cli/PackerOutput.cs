namespace Packer;

internal sealed class PackerOutput(TextWriter output, TextWriter error) : IProgress<DownloadProgress>
{
    public void WriteLine(string message)
    {
        output.WriteLine(message);
    }

    public void WriteError(string message)
    {
        error.WriteLine(message);
    }

    public void ReportProgress(PackBuilderProgress progress)
    {
        WriteProgress(progress.Processed, progress.Total);
    }

    public void ReportProgress(PackEnhancerProgress progress)
    {
        switch (progress)
        {
            case PackEnhancerProgress.Selected data:
                WriteLine(
                    $"Selected {data.EntryCount} entries: " +
                    $"{data.UpscaleCount} upscale, " +
                    $"{data.RefineCount} refine, " +
                    $"{data.AlphaCount} alpha, " +
                    $"{data.SurfaceCount} surface.");
                break;

            case PackEnhancerProgress.Written data:
                WriteProgress(data.Processed, data.Total);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(progress), progress, null);
        }
    }

    public void Report(DownloadProgress progress)
    {
        output.WriteLine(
            $"  {progress.Name}: {FormatSize(progress.Completed)} of {FormatSize(progress.Total)}"
          + $" from {progress.Host}...");
    }

    public void ReportMissing(PreviewBuilderResult.MissingFrames result)
    {
        error.WriteLine($"Missing {result.Frames.Count} frames from the enhancement cache:");

        foreach (string frame in result.Frames)
        {
            error.WriteLine($"  {frame}");
        }
    }

    public void ReportMissing(PackBuilderResult.MissingRequired result)
    {
        error.WriteLine($"Missing {result.AssetIds.Count} required assets:");

        foreach (string id in result.AssetIds)
        {
            error.WriteLine($"  {id}");
        }
    }

    private static string FormatSize(long bytes)
    {
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }

    private void WriteProgress(int processed, int total)
    {
        output.WriteLine($"  {processed}/{total} keys processed...");
    }
}
