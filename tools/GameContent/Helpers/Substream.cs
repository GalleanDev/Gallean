namespace GameContent;

internal class Substream : Stream
{
    private readonly long baseOffset;
    private readonly Stream baseStream;
    private readonly long length;
    private long position;

    public Substream(Stream baseStream, long baseOffset, long length)
    {
        if (!baseStream.CanRead)
        {
            throw new ArgumentException("Base stream must be readable.");
        }

        if (!baseStream.CanSeek)
        {
            throw new ArgumentException("Base stream must be seekable.");
        }

        this.baseStream = baseStream;
        this.baseOffset = baseOffset;
        this.length = length;
        position = 0;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => length;

    public override long Position
    {
        get => position;
        set
        {
            if (value < 0 || value > length)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }

            position = value;
        }
    }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count)
    {
        long remaining = length - position;

        if (remaining > 0)
        {
            int toRead = (int)Math.Min(count, remaining);

            baseStream.Position = baseOffset + position;
            int read = baseStream.Read(buffer, offset, toRead);

            position += read;
            return read;
        }
        else
        {
            return 0;
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        long newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => position + offset,
            SeekOrigin.End => length + offset
        };

        Position = newPosition;
        return position;
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
