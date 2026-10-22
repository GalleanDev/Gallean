using System.IO.Compression;
using System.Security.Cryptography;

namespace Packer;

internal sealed class NativeLibraryStore(
    NuGetClient client,
    DownloadStore store,
    string baseDirectory,
    string rootDirectory)
{
    private const string RuntimesDirectoryName = "runtimes";
    private const string FragmentsDirectoryName = "fragments";
    private const string NativeDirectoryName = "native";
    private const string PartialExtension = ".partial";
    private const string ShaExtension = ".sha";
    private const string FragmentExtension = ".fragment";

    private static readonly string[] Runtimes = ["linux-x64", "win-x64"];

    private static readonly string[] PayloadPrefixes =
    [
        $"{RuntimesDirectoryName}/{NativeRuntime.Identifier}/{NativeDirectoryName}/",
        $"{FragmentsDirectoryName}/{NativeRuntime.Identifier}/{NativeDirectoryName}/"
    ];

    public async Task<string> EnsureLibraryAsync(NativeLibrarySet set, CancellationToken cancellationToken)
    {
        string? existingDirectory = FindLibrary(set);

        if (existingDirectory is not null)
        {
            return existingDirectory;
        }

        if (!Runtimes.Contains(NativeRuntime.Identifier))
        {
            throw new NotSupportedException($"The native libraries for {NativeRuntime.Identifier} are not available.");
        }

        string cacheDirectory = GetCacheDirectory(set);

        await AssembleAsync(set, cacheDirectory, cancellationToken);

        return cacheDirectory;
    }

    public string? FindLibrary(NativeLibrarySet set)
    {
        foreach (string directory in GetCandidateDirectories(set))
        {
            if (HasFiles(directory, set.Files))
            {
                return directory;
            }
        }

        return null;
    }

    private async Task AssembleAsync(NativeLibrarySet set, string directory, CancellationToken cancellationToken)
    {
        List<string> packagePaths = await DownloadPackagesAsync(set, cancellationToken);

        string staging = $"{directory}.{Environment.ProcessId}{PartialExtension}";

        try
        {
            Directory.CreateDirectory(staging);

            foreach (string packagePath in packagePaths)
            {
                ExtractPayload(packagePath, staging);
            }

            CombineFragments(staging);

            foreach (string file in set.Files)
            {
                if (!File.Exists(Path.Combine(staging, file)))
                {
                    throw new InvalidDataException($"No package contains '{file}' for {NativeRuntime.Identifier}.");
                }
            }

            if (!Directory.Exists(directory))
            {
                Directory.Move(staging, directory);
            }
        }
        finally
        {
            if (Directory.Exists(staging))
            {
                Directory.Delete(staging, recursive: true);
            }
        }

        foreach (string packagePath in packagePaths)
        {
            File.Delete(packagePath);
        }
    }

    private async Task<List<string>> DownloadPackagesAsync(NativeLibrarySet set, CancellationToken cancellationToken)
    {
        List<string> paths = [];

        foreach (NuGetPackage package in set.Packages)
        {
            string packagePath = await DownloadPackageAsync(package, cancellationToken);

            paths.Add(packagePath);

            foreach (NuGetPackage part in GetParts(package, packagePath))
            {
                string partPath = await DownloadPackageAsync(part, cancellationToken);

                paths.Add(partPath);
            }
        }

        return paths;
    }

    private async Task<string> DownloadPackageAsync(NuGetPackage package, CancellationToken cancellationToken)
    {
        RemoteFile file = await client.GetRemoteFileAsync(package, cancellationToken);

        return await store.EnsureAsync(file, cancellationToken);
    }

    private static List<NuGetPackage> GetParts(NuGetPackage package, string packagePath)
    {
        IReadOnlyList<NuGetPackage> dependencies = NuGetClient.ReadDependencies(packagePath);
        List<NuGetPackage> parts = [];

        foreach (NuGetPackage dependency in dependencies)
        {
            if (dependency.Id.StartsWith($"{package.Id}-", StringComparison.OrdinalIgnoreCase))
            {
                parts.Add(dependency);
            }
        }

        return parts;
    }

    private static void ExtractPayload(string packagePath, string directory)
    {
        using ZipArchive archive = ZipFile.OpenRead(packagePath);

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            string? name = GetPayloadName(entry.FullName);

            if (name is not null)
            {
                entry.ExtractToFile(Path.Combine(directory, name), overwrite: true);
            }
        }
    }

    private static string? GetPayloadName(string entryName)
    {
        foreach (string prefix in PayloadPrefixes)
        {
            if (entryName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                string name = entryName[prefix.Length..];

                if (name.Length > 0 && !name.Contains('/'))
                {
                    return name;
                }
            }
        }

        return null;
    }

    private static void CombineFragments(string directory)
    {
        foreach (string shaPath in Directory.GetFiles(directory, $"*{ShaExtension}"))
        {
            string path = shaPath[..^ShaExtension.Length];
            string expected = File.ReadAllText(shaPath);

            AppendFragments(path);
            RequireHash(path, expected.Trim());

            File.Delete(shaPath);
        }
    }

    private static void AppendFragments(string path)
    {
        using FileStream target = new(path, FileMode.Append, FileAccess.Write);

        for (int index = 1;; index++)
        {
            string fragmentPath = $"{path}{FragmentExtension}{index}";

            if (!File.Exists(fragmentPath))
            {
                return;
            }

            using (FileStream fragment = File.OpenRead(fragmentPath))
            {
                fragment.CopyTo(target);
            }

            File.Delete(fragmentPath);
        }
    }

    private static void RequireHash(string path, string expected)
    {
        using FileStream stream = File.OpenRead(path);

        byte[] hash = SHA256.HashData(stream);
        string actual = Convert.ToHexStringLower(hash);

        if (actual != expected)
        {
            throw new InvalidDataException($"Unexpected hash of '{Path.GetFileName(path)}' (actual: {actual}, expected: {expected}).");
        }
    }

    private static bool HasFiles(string directory, IReadOnlyList<string> files)
    {
        foreach (string file in files)
        {
            if (!File.Exists(Path.Combine(directory, file)))
            {
                return false;
            }
        }

        return true;
    }

    private IEnumerable<string> GetCandidateDirectories(NativeLibrarySet set)
    {
        yield return baseDirectory;
        yield return Path.Combine(baseDirectory, RuntimesDirectoryName, NativeRuntime.Identifier, NativeDirectoryName);
        yield return GetCacheDirectory(set);
    }

    private string GetCacheDirectory(NativeLibrarySet set)
    {
        return Path.Combine(rootDirectory, NativeRuntime.Identifier, set.Name);
    }
}
