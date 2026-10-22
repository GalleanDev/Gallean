using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace Packer;

internal sealed class NuGetClient(HttpClient client)
{
    private const string FlatContainer = "https://api.nuget.org/v3-flatcontainer/";
    private const string Sha512HeaderName = "x-ms-meta-sha512";
    private const string NuspecExtension = ".nuspec";
    private const string DependencyName = "dependency";
    private const string IdAttributeName = "id";
    private const string VersionAttributeName = "version";

    public static string GetFileName(NuGetPackage package)
    {
        return $"{package.Id.ToLowerInvariant()}.{package.Version.ToLowerInvariant()}.nupkg";
    }

    public static IReadOnlyList<NuGetPackage> ReadDependencies(string packagePath)
    {
        using ZipArchive archive = ZipFile.OpenRead(packagePath);

        ZipArchiveEntry? nuspec = FindNuspec(archive);

        if (nuspec is null)
        {
            throw new InvalidDataException($"The package '{packagePath}' has no nuspec.");
        }

        using Stream contents = nuspec.Open();

        XDocument document = XDocument.Load(contents);
        List<NuGetPackage> dependencies = [];

        foreach (XElement element in document.Descendants())
        {
            if (element.Name.LocalName == DependencyName)
            {
                string? id = element.Attribute(IdAttributeName)?.Value;
                string? version = element.Attribute(VersionAttributeName)?.Value;

                if (id is not null && version is not null && !Contains(dependencies, id))
                {
                    dependencies.Add(new NuGetPackage(id, version));
                }
            }
        }

        return dependencies;
    }

    public async Task<RemoteFile> GetRemoteFileAsync(NuGetPackage package, CancellationToken cancellationToken)
    {
        string fileName = GetFileName(package);
        Uri location = GetLocation(package, fileName);

        using HttpRequestMessage request = new(HttpMethod.Head, location);
        using HttpResponseMessage response = await client.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"'{location}' returned {(int)response.StatusCode} {response.StatusCode}.");
        }

        long? length = response.Content.Headers.ContentLength;

        if (length is null)
        {
            throw new InvalidDataException($"'{location}' has no content length.");
        }

        return new RemoteFile(fileName, location, length.Value, ReadDigest(response), null);
    }

    private static Uri GetLocation(NuGetPackage package, string fileName)
    {
        string id = package.Id.ToLowerInvariant();
        string version = package.Version.ToLowerInvariant();

        return new Uri($"{FlatContainer}{id}/{version}/{fileName}");
    }

    private static DownloadDigest? ReadDigest(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues(Sha512HeaderName, out IEnumerable<string>? values))
        {
            string? encoded = values.FirstOrDefault();

            if (encoded is not null)
            {
                return DownloadDigest.FromBase64(HashAlgorithmName.SHA512, encoded);
            }
        }

        byte[]? contentMd5 = response.Content.Headers.ContentMD5;

        if (contentMd5 is null)
        {
            return null;
        }

        return DownloadDigest.FromBytes(HashAlgorithmName.MD5, contentMd5);
    }

    private static ZipArchiveEntry? FindNuspec(ZipArchive archive)
    {
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (entry.FullName.EndsWith(NuspecExtension, StringComparison.OrdinalIgnoreCase) &&
                !entry.FullName.Contains('/'))
            {
                return entry;
            }
        }

        return null;
    }

    private static bool Contains(IReadOnlyList<NuGetPackage> packages, string id)
    {
        foreach (NuGetPackage package in packages)
        {
            if (package.Id == id)
            {
                return true;
            }
        }

        return false;
    }
}
