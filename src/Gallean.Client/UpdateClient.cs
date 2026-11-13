using System.Buffers;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;

namespace Gallean.Client;

internal sealed class UpdateClient : IDisposable
{
    private const int DefaultDownloadBufferSize = 64 * 1024;
    private static readonly Uri LatestReleaseUri = new("https://gallean.org/go/3");
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private readonly HttpClient httpClient;
    private readonly string downloadDirectory;
    private readonly ILog log;

    public UpdateClient(ClientVersion clientVersion, string downloadDirectory, ILog log)
    {
        this.downloadDirectory = downloadDirectory;
        this.log = log;

        httpClient = new HttpClient
        {
            Timeout = DefaultTimeout
        };

        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Gallean", clientVersion.Version));
    }

    public async Task<UpdateInfo?> TryGetLatestAsync()
    {
        Uri? releaseUri;

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Head, LatestReleaseUri);
            using HttpResponseMessage response = await httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();
            releaseUri = response.RequestMessage?.RequestUri;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            log.Error("Failed to check for updates.", ex);

            return null;
        }

        if (releaseUri?.Segments is [.., "tag/", ['v', .. string version]])
        {
            Uri packageUri = new($"https://gallean.org/go/4/{version}/{RuntimeInformation.RuntimeIdentifier}");

            return new UpdateInfo(new ClientVersion(version, null), releaseUri, packageUri);
        }
        else
        {
            return null;
        }
    }

    public async Task<string> DownloadPackageAsync(UpdateInfo update, IProgress<double> progress, CancellationToken cancellationToken)
    {
        string path = Path.Combine(downloadDirectory, $"Gallean-{update.Version.Version}-{RuntimeInformation.RuntimeIdentifier}.zip");

        Directory.CreateDirectory(downloadDirectory);
        await DownloadFileAsync(update.PackageUri, path, progress, cancellationToken);

        return path;
    }

    private async Task DownloadFileAsync(Uri uri, string path, IProgress<double> progress, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        response.EnsureSuccessStatusCode();

        await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using FileStream target = new(path, FileMode.Create, FileAccess.Write, FileShare.None);

        long? length = response.Content.Headers.ContentLength;

        if (length > 0)
        {
            await CopyAsync(source, target, length.Value, DefaultDownloadBufferSize, progress, cancellationToken);
        }
        else
        {
            await source.CopyToAsync(target, cancellationToken);
        }
    }

    private static async Task CopyAsync(
        Stream source,
        Stream target,
        long length,
        int bufferSize,
        IProgress<double> progress,
        CancellationToken cancellationToken)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

        try
        {
            long position = 0;
            int count = await source.ReadAsync(buffer, cancellationToken);

            while (count > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, count), cancellationToken);

                position += count;
                progress.Report((double)position / length);

                count = await source.ReadAsync(buffer, cancellationToken);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}
