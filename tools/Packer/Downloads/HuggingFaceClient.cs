using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace Packer;

internal sealed class HuggingFaceClient(HttpClient client, string? token)
{
    private const string Host = "https://huggingface.co/";
    private const string SizeHeaderName = "x-linked-size";
    private const string DigestHeaderName = "x-linked-etag";
    private const int Sha256HexLength = 64;

    public async Task<RemoteFile> GetRemoteFileAsync(HuggingFaceFile file, CancellationToken cancellationToken)
    {
        Uri location = GetLocation(file);

        AuthenticationHeaderValue? authorization = BuildAuthorization(token);

        using HttpRequestMessage request = new(HttpMethod.Head, location);
        request.Headers.Authorization = authorization;

        using HttpResponseMessage response = await client.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!IsStored(response) && !response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"'{location}' returned {(int)response.StatusCode} {response.StatusCode}.");
        }

        long length = ReadLength(response, location);

        return new RemoteFile(GetName(file), location, length, ReadDigest(response), authorization);
    }

    private static AuthenticationHeaderValue? BuildAuthorization(string? token)
    {
        if (!string.IsNullOrEmpty(token))
        {
            return new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            return null;
        }
    }

    private static Uri GetLocation(HuggingFaceFile file)
    {
        return new Uri($"{Host}{file.Repository}/resolve/{file.Revision}/{file.Path}");
    }

    private static string GetName(HuggingFaceFile file)
    {
        int separator = file.Path.LastIndexOf('/');

        return file.Path[(separator + 1)..];
    }

    private static bool IsStored(HttpResponseMessage response)
    {
        int status = (int)response.StatusCode;

        return status is >= 300 and < 400 && response.Headers.Location is not null;
    }

    private static long ReadLength(HttpResponseMessage response, Uri location)
    {
        if (IsStored(response))
        {
            return ReadStoredLength(response, location);
        }
        else
        {
            return ReadContentLength(response, location);
        }
    }

    private static long ReadStoredLength(HttpResponseMessage response, Uri location)
    {
        if (response.Headers.TryGetValues(SizeHeaderName, out IEnumerable<string>? values))
        {
            string? size = values.FirstOrDefault();

            if (size is not null && long.TryParse(size, out long length))
            {
                return length;
            }
        }

        throw new InvalidDataException($"'{location}' has no {SizeHeaderName}.");
    }

    private static long ReadContentLength(HttpResponseMessage response, Uri location)
    {
        long? length = response.Content.Headers.ContentLength;

        if (length is null)
        {
            throw new InvalidDataException($"'{location}' has no content length.");
        }

        return length.Value;
    }

    private static DownloadDigest? ReadDigest(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues(DigestHeaderName, out IEnumerable<string>? values))
        {
            return null;
        }

        string? encoded = values.FirstOrDefault();

        if (encoded is null)
        {
            return null;
        }

        string hex = encoded.Trim('"');

        if (hex.Length != Sha256HexLength)
        {
            return null;
        }

        return DownloadDigest.FromHex(HashAlgorithmName.SHA256, hex);
    }
}
