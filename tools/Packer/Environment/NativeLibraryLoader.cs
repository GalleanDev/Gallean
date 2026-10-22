using System.Runtime.InteropServices;

namespace Packer;

internal sealed class NativeLibraryLoader(NativeLibraryStore libraries)
{
    public void UseImageMagick()
    {
        NativeLibrarySet set = NativeLibrarySet.ImageMagick();
        string? libraryDirectory = libraries.FindLibrary(set);

        if (libraryDirectory is null)
        {
            throw new InvalidOperationException($"The native libraries for '{set.Name}' are missing.");
        }

        NativeLibrary.Load(Path.Combine(libraryDirectory, ImageMagickNativeLibrary.FileName));
    }

    public void UseTorch(UmbraDevice device)
    {
        NativeLibrarySet set = NativeLibrarySet.Torch(device);
        string? libraryDirectory = libraries.FindLibrary(set);

        if (libraryDirectory is null)
        {
            throw new InvalidOperationException($"The native libraries for '{set.Name}' are missing.");
        }

        UmbraNativeLibraries.Use(libraryDirectory, device);
    }
}
