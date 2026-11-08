using System.Net.Http.Headers;

namespace Gallean.Client;

internal sealed class UpdateClient : IDisposable
{
    private static readonly Uri LatestReleaseUri = new("https://gallean.org/go/3");
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private readonly HttpClient httpClient;
    private readonly ILog log;

    public UpdateClient(ClientVersion clientVersion, ILog log)
    {
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
            return new UpdateInfo(new ClientVersion(version, null), releaseUri);
        }
        else
        {
            return null;
        }
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}
