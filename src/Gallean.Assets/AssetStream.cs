using System.Runtime.InteropServices;

namespace Gallean.Assets;

public sealed class AssetStream(Func<Stream> openFunc)
{
    public Stream Open()
    {
        return openFunc();
    }

    public static AssetStream FromBytes(ReadOnlyMemory<byte> bytes)
    {
        return new AssetStream(() =>
        {
            if (MemoryMarshal.TryGetArray(bytes, out ArraySegment<byte> segment) && segment.Array is not null)
            {
                return new MemoryStream(segment.Array, segment.Offset, segment.Count, writable: false);
            }

            return new MemoryStream(bytes.ToArray(), writable: false);
        });
    }

    public static AssetStream FromFile(string path)
    {
        return new AssetStream(() => File.OpenRead(path));
    }
}
