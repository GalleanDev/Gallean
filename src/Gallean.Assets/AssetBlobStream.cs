using Microsoft.Win32.SafeHandles;

namespace Gallean.Assets;

internal sealed class AssetBlobStream(SafeFileHandle handle, long start, int length) : Stream
{
    private int position;

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => length;

    public override long Position
    {
        get => position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);

        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        int remaining = length - position;

        if (remaining <= 0)
        {
            return 0;
        }

        if (buffer.Length > remaining)
        {
            buffer = buffer[..remaining];
        }

        int read = RandomAccess.Read(handle, buffer, start + position);
        position += read;

        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        long target = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => position + offset,
            SeekOrigin.End => length + offset
        };

        if (target < 0 || target > length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, null);
        }

        position = (int)target;

        return position;
    }

    public override void Flush()
    {
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}
