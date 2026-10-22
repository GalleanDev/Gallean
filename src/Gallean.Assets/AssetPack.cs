using System.Buffers.Binary;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

namespace Gallean.Assets;

public sealed partial class AssetPack : IDisposable
{
    public const int CurrentFormat = 27;
    private const int HeaderLength = 32;
    private const int MaxManifestLength = 256 * 1024 * 1024;

    private static readonly byte[] Magic = "GALLEANPACK"u8.ToArray();

    private readonly SafeFileHandle handle;

    private AssetPack(SafeFileHandle handle, AssetManifest manifest)
    {
        this.handle = handle;

        Manifest = manifest;
    }

    public AssetManifest Manifest { get; }

    public void Dispose()
    {
        handle.Dispose();
    }

    public ReadOnlyMemory<byte> ReadSprite(AssetManifestSprite sprite)
    {
        return ReadBlob(sprite.Offset, sprite.Length);
    }

    public ReadOnlyMemory<byte> ReadAnimationFrame(AssetManifestAnimationFrame frame)
    {
        return ReadBlob(frame.Offset, frame.Length);
    }

    public ReadOnlyMemory<byte> ReadSpriteLayer(AssetManifestSpriteLayer layer)
    {
        return ReadBlob(layer.Offset, layer.Length);
    }

    public AssetStream OpenSprite(AssetManifestSprite sprite)
    {
        return OpenBlob(sprite.Offset, sprite.Length);
    }

    public AssetStream OpenSound(AssetManifestSound sound)
    {
        return OpenBlob(sound.Offset, sound.Length);
    }

    private ReadOnlyMemory<byte> ReadBlob(long offset, int length)
    {
        byte[] buffer = new byte[length];

        ReadExactly(handle, buffer, HeaderLength + offset);

        return buffer;
    }

    private AssetStream OpenBlob(long offset, int length)
    {
        return new AssetStream(() => new AssetBlobStream(handle, HeaderLength + offset, length));
    }

    private static void ReadExactly(SafeFileHandle handle, Span<byte> buffer, long offset)
    {
        int read = 0;

        while (read < buffer.Length)
        {
            int current = RandomAccess.Read(handle, buffer[read..], offset + read);

            if (current <= 0)
            {
                throw new EndOfStreamException();
            }

            read += current;
        }
    }

    public static AssetPack Read(string path)
    {
        SafeFileHandle handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.RandomAccess);

        try
        {
            if (RandomAccess.GetLength(handle) < HeaderLength)
            {
                throw new InvalidDataException("The data is not a valid Gallean asset pack.");
            }

            byte[] header = new byte[HeaderLength];

            ReadExactly(handle, header, 0);

            if (!header.AsSpan(0, Magic.Length).SequenceEqual(Magic))
            {
                throw new InvalidDataException("The data is not a valid Gallean asset pack.");
            }

            int format = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(12));

            if (format != CurrentFormat)
            {
                throw new AssetPackUnsupportedFormatException($"Unsupported asset pack format '{format}'.");
            }

            long manifestOffset = BinaryPrimitives.ReadInt64LittleEndian(header.AsSpan(16));
            long manifestLength = BinaryPrimitives.ReadInt64LittleEndian(header.AsSpan(24));
            long fileLength = RandomAccess.GetLength(handle);

            if (manifestOffset < HeaderLength || manifestOffset > fileLength)
            {
                throw new InvalidDataException($"The asset pack manifest offset '{manifestOffset}' is out of range.");
            }

            if (manifestLength < 0 || manifestLength > MaxManifestLength || manifestOffset > fileLength - manifestLength)
            {
                throw new InvalidDataException($"The asset pack manifest length '{manifestLength}' is out of range.");
            }

            byte[] manifestBytes = new byte[manifestLength];

            ReadExactly(handle, manifestBytes, manifestOffset);

            AssetManifest? manifest = JsonSerializer.Deserialize(manifestBytes, AssetPackJsonContext.Default.AssetManifest);

            if (manifest is null)
            {
                throw new InvalidDataException("The asset pack manifest is empty.");
            }

            return new AssetPack(handle, manifest);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }
}
