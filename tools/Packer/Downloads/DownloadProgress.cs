namespace Packer;

internal sealed record DownloadProgress(string Name, string Host, long Completed, long Total);
