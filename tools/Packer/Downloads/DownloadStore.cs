using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;

namespace Packer;

internal sealed class DownloadStore(HttpClient client, IProgress<DownloadProgress> progress, string directory)
{
    private const string PartialExtension = ".partial";
    private const int BufferSize = 1024 * 1024;
    private const int MaxRedirects = 8;

    private static readonly TimeSpan ProgressInterval = TimeSpan.FromSeconds(5);

    public async Task<string> EnsureAsync(RemoteFile file, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(directory);

        string targetPath = Path.Combine(directory, file.Name);

        if (File.Exists(targetPath))
        {
            return targetPath;
        }

        string partialPath = targetPath + PartialExtension;

        await ReceiveAsync(file, partialPath, cancellationToken);
        await VerifyAsync(file, partialPath, cancellationToken);

        File.Move(partialPath, targetPath, overwrite: true);

        return targetPath;
    }

    private static long GetResumeOffset(string partialPath, long expectedLength)
    {
        if (!File.Exists(partialPath))
        {
            return 0;
        }

        FileInfo info = new(partialPath);

        if (info.Length > expectedLength)
        {
            return 0;
        }
        else
        {
            return info.Length;
        }
    }

    private static Uri? GetRedirect(HttpResponseMessage response)
    {
        int status = (int)response.StatusCode;

        if (status is >= 300 and < 400)
        {
            return response.Headers.Location;
        }
        else
        {
            return null;
        }
    }

    private async Task ReceiveAsync(RemoteFile file, string partialPath, CancellationToken cancellationToken)
    {
        long requested = GetResumeOffset(partialPath, file.Length);

        if (requested == file.Length)
        {
            return;
        }

        using HttpResponseMessage response = await OpenAsync(file, requested, cancellationToken);

        long offset = 0;
        FileMode mode = FileMode.Create;

        if (response.StatusCode == HttpStatusCode.PartialContent)
        {
            offset = requested;
            mode = FileMode.Append;
        }

        DownloadProgress started = new(file.Name, GetHost(response, file), offset, file.Length);

        progress.Report(started);

        await using Stream contents = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using FileStream target = new(partialPath, mode, FileAccess.Write, FileShare.None, BufferSize);

        await CopyAsync(contents, target, started, cancellationToken);
    }

    private static string GetHost(HttpResponseMessage response, RemoteFile file)
    {
        Uri? location = response.RequestMessage?.RequestUri;

        if (location is not null)
        {
            return location.Host;
        }
        else
        {
            return file.Location.Host;
        }
    }

    private async Task<HttpResponseMessage> OpenAsync(RemoteFile file, long offset, CancellationToken cancellationToken)
    {
        Uri location = file.Location;

        for (int index = 0; index <= MaxRedirects; index++)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, location);

            if (offset > 0)
            {
                request.Headers.Range = new RangeHeaderValue(offset, null);
            }

            if (location.Host == file.Location.Host)
            {
                request.Headers.Authorization = file.Authorization;
            }

            HttpResponseMessage response = await client.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            Uri? redirect = GetRedirect(response);

            if (redirect is null)
            {
                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                HttpStatusCode status = response.StatusCode;

                response.Dispose();

                throw new HttpRequestException($"'{location}' returned {(int)status} {status}.");
            }

            location = new Uri(location, redirect);
            response.Dispose();
        }

        throw new HttpRequestException($"'{file.Location}' redirected more than {MaxRedirects} times.");
    }

    private async Task CopyAsync(
        Stream contents,
        Stream target,
        DownloadProgress started,
        CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[BufferSize];
        Stopwatch reported = Stopwatch.StartNew();
        long completed = started.Completed;

        while (true)
        {
            int read = await contents.ReadAsync(buffer, cancellationToken);

            if (read <= 0)
            {
                return;
            }

            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            completed += read;

            if (reported.Elapsed >= ProgressInterval)
            {
                progress.Report(started with { Completed = completed });
                reported.Restart();
            }
        }
    }

    private static async Task VerifyAsync(RemoteFile file, string path, CancellationToken cancellationToken)
    {
        FileInfo info = new(path);

        if (info.Length != file.Length)
        {
            File.Delete(path);

            throw new InvalidDataException($"'{file.Location}' returned {info.Length} bytes, expected {file.Length}.");
        }

        if (file.Digest is not null)
        {
            bool matches = await file.Digest.MatchesAsync(path, cancellationToken);

            if (!matches)
            {
                File.Delete(path);

                throw new InvalidDataException($"The {file.Digest.Algorithm} of '{file.Location}' does not match.");
            }
        }
    }
}
