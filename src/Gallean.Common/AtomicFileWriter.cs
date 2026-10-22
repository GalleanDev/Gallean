namespace Gallean.Common;

public sealed class AtomicFileWriter : IDisposable
{
    private const int DefaultBufferSize = 4096;

    private readonly bool flushToDisk;
    private readonly string targetPath;
    private readonly string temporaryPath;
    private bool committed;

    public AtomicFileWriter(
        string targetPath,
        FileOptions options = FileOptions.None,
        int bufferSize = DefaultBufferSize,
        bool flushToDisk = false)
    {
        this.targetPath = targetPath;
        this.flushToDisk = flushToDisk;
        temporaryPath = $"{targetPath}.tmp";

        Stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, options);
    }

    public FileStream Stream { get; }

    public void Dispose()
    {
        if (!committed)
        {
            Stream.Dispose();
            File.Delete(temporaryPath);
        }
    }

    public void Commit()
    {
        if (!committed)
        {
            if (flushToDisk)
            {
                Stream.Flush(flushToDisk: true);
            }

            Stream.Dispose();
            File.Move(temporaryPath, targetPath, overwrite: true);
            committed = true;
        }
    }

    public async Task CommitAsync()
    {
        if (!committed)
        {
            if (flushToDisk)
            {
                await Stream.FlushAsync();
                Stream.Flush(flushToDisk: true);
            }

            await Stream.DisposeAsync();
            File.Move(temporaryPath, targetPath, overwrite: true);
            committed = true;
        }
    }
}
