namespace Packer;

internal sealed record NativeLibrarySet(
    string Name,
    IReadOnlyList<NuGetPackage> Packages,
    IReadOnlyList<string> Files)
{
    private const string MagickPackageId = "Magick.NET-Q8-AnyCPU";
    private const string TorchSharpPackageId = "TorchSharp";

    public static IReadOnlyList<NativeLibrarySet> All(UmbraDevice device)
    {
        return [ImageMagick(), Torch(device)];
    }

    public static NativeLibrarySet ImageMagick()
    {
        NuGetPackage package = new(MagickPackageId, NativePackageVersions.ImageMagick);

        return new NativeLibrarySet($"magick-{package.Version}", [package], [ImageMagickNativeLibrary.FileName]);
    }

    public static NativeLibrarySet Torch(UmbraDevice device)
    {
        NuGetPackage backend = GetBackendPackage(device);
        NuGetPackage bindings = new(TorchSharpPackageId, NativePackageVersions.TorchSharp);

        return new NativeLibrarySet(
            $"{backend.Id}-{backend.Version}-torchsharp-{bindings.Version}",
            [backend, bindings],
            UmbraNativeLibraries.GetRequiredFiles(device));
    }

    private static NuGetPackage GetBackendPackage(UmbraDevice device)
    {
        string id = device switch
        {
            UmbraDevice.Cpu => $"libtorch-cpu-{NativeRuntime.Identifier}",
            UmbraDevice.Gpu => $"libtorch-cuda-{NativePackageVersions.LibTorchCuda}-{NativeRuntime.Identifier}"
        };

        return new NuGetPackage(id, NativePackageVersions.LibTorch);
    }
}
