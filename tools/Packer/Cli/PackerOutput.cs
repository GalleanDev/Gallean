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
        output.WriteLine(FormatProgress(progress.Processed, progress.Total));
    }

    public void ReportProgress(PackEnhancerProgress progress)
    {
        string message = progress switch
        {
            PackEnhancerProgress.Selected selected => FormatSelected(selected),
            PackEnhancerProgress.Written written => FormatProgress(written.Processed, written.Total)
        };

        output.WriteLine(message);
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

    private static string FormatSelected(PackEnhancerProgress.Selected progress)
    {
        return $"Selected {progress.EntryCount} entries: " +
               $"{progress.UpscaleCount} upscale, " +
               $"{progress.RefineCount} refine, " +
               $"{progress.AlphaCount} alpha, " +
               $"{progress.SurfaceCount} surface.";
    }

    private static string FormatProgress(int processed, int total)
    {
        return $"  {processed}/{total} keys processed...";
    }
}
