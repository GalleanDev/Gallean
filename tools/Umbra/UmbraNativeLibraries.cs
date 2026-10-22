using System.Runtime.InteropServices;

namespace Umbra;

public static class UmbraNativeLibraries
{
    private const string TorchSharpImportName = "LibTorchSharp";
    private const string CpuBackendName = "torch_cpu";
    private const string CudaBackendName = "torch_cuda";

    public static IReadOnlyList<string> GetRequiredFiles(UmbraDevice device)
    {
        return [GetFileName(GetBackendName(device)), GetFileName(TorchSharpImportName)];
    }

    public static void Use(string libraryDirectory, UmbraDevice device)
    {
        foreach (string file in GetRequiredFiles(device))
        {
            NativeLibrary.Load(Path.Combine(libraryDirectory, file));
        }
    }

    private static string GetBackendName(UmbraDevice device)
    {
        return device switch
        {
            UmbraDevice.Cpu => CpuBackendName,
            UmbraDevice.Gpu => CudaBackendName
        };
    }

    private static string GetFileName(string importName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return $"{importName}.dll";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return $"lib{importName}.dylib";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return $"lib{importName}.so";
        }
        else
        {
            throw new PlatformNotSupportedException($"The platform '{RuntimeInformation.OSDescription}' is not supported.");
        }
    }
}
