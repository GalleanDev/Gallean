using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace Gallean.Client;

internal sealed class UpdateInstaller(string installDirectory)
{
    private readonly string tempDirectory = Path.Combine(installDirectory, ".tmp");
    private readonly string pendingDirectory = Path.Combine(installDirectory, ".tmp", "pending");

    public bool CanInstall => !RuntimeFeature.IsDynamicCodeSupported;

    public void Install(string packagePath)
    {
        string packageName = Path.GetFileNameWithoutExtension(packagePath);
        string stagingDirectory = Path.Combine(tempDirectory, Path.GetRandomFileName());

        DirectoryInfo tempDirectoryInfo = Directory.CreateDirectory(tempDirectory);
        tempDirectoryInfo.Attributes |= FileAttributes.Hidden;

        ApplyPending();

        ZipFile.ExtractToDirectory(packagePath, stagingDirectory);
        Directory.Move(Path.Combine(stagingDirectory, packageName), pendingDirectory);

        ApplyPending();
    }

    public void Cleanup()
    {
        ApplyPending();

        if (Directory.Exists(tempDirectory))
        {
            foreach (string directory in Directory.GetDirectories(tempDirectory))
            {
                TryDelete(directory);
            }

            TryDelete(tempDirectory);
        }
    }

    private void ApplyPending()
    {
        if (Directory.Exists(pendingDirectory))
        {
            string prevDirectory = Path.Combine(tempDirectory, Path.GetRandomFileName());

            Directory.CreateDirectory(prevDirectory);

            foreach (string path in Directory.GetFileSystemEntries(pendingDirectory))
            {
                string name = Path.GetFileName(path);
                string installedPath = Path.Combine(installDirectory, name);

                if (Path.Exists(installedPath))
                {
                    Move(installedPath, Path.Combine(prevDirectory, name));
                }

                Move(path, installedPath);
            }

            Directory.Delete(pendingDirectory);
        }
    }

    private static void Move(string sourcePath, string targetPath)
    {
        if (Directory.Exists(sourcePath))
        {
            Directory.Move(sourcePath, targetPath);
        }
        else
        {
            File.Move(sourcePath, targetPath);
        }
    }

    private static void TryDelete(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
