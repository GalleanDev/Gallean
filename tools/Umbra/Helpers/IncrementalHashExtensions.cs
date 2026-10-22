using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Umbra;

internal static class IncrementalHashExtensions
{
    extension(IncrementalHash hash)
    {
        public void AppendInt32(int value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            hash.AppendData(buffer);
        }
    }
}
