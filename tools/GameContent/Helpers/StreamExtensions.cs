using System.Buffers.Binary;
using System.Text;

namespace GameContent;

internal static class StreamExtensions
{
    extension(Stream stream)
    {
        public void Skip(int length)
        {
            stream.Seek(length, SeekOrigin.Current);
        }

        public short ReadInt16LittleEndian()
        {
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadInt16LittleEndian(buffer);
        }

        public int ReadInt32LittleEndian()
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }

        public long ReadInt64LittleEndian()
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadInt64LittleEndian(buffer);
        }

        public string ReadAsciiString(int length)
        {
            Span<byte> buffer = length <= 1024 ? stackalloc byte[length] : new byte[length];
            stream.ReadExactly(buffer);

            int stringEnd = buffer.IndexOf((byte)'\0');
            if (stringEnd == 0)
            {
                return string.Empty;
            }

            if (stringEnd != -1)
            {
                buffer = buffer[..stringEnd];
            }

            return Encoding.ASCII.GetString(buffer);
        }

        public string ReadAsciiString()
        {
            StringBuilder resultString = new();
            while (true)
            {
                int ch = stream.ReadByte();
                if (ch == -1 || ch == 0)
                {
                    break;
                }

                resultString.Append((char)ch);
            }

            return resultString.ToString();
        }
    }
}
