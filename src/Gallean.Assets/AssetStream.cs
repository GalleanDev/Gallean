namespace Gallean.Assets;

public sealed class AssetStream(Func<Stream> openFunc)
{
    public Stream Open()
    {
        return openFunc();
    }

    public static AssetStream FromBytes(ReadOnlyMemory<byte> bytes)
    {
        return new AssetStream(() => new ReadOnlyMemoryStream(bytes));
    }

    public static AssetStream FromFile(string path)
    {
        return new AssetStream(() => File.OpenRead(path));
    }
}
