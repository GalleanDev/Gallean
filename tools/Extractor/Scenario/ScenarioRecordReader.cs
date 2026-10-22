using System.Buffers.Binary;

namespace Extractor;

internal sealed class ScenarioRecordReader(Stream stream, ScenarioTextEncoding encoding)
{
    private const string EmptyId = "G000000000";

    private const int IdScenarioNumberLength = 4;
    private const int MaxFieldNameLength = 64;

    private string? targetScenarioNumber;

    public long Position => stream.Position;
    public long Length => stream.Length;

    public void Seek(long position)
    {
        stream.Seek(position, SeekOrigin.Begin);
    }

    public void Skip(int length)
    {
        stream.Seek(length, SeekOrigin.Current);
    }

    public byte[] ReadBytes(int length)
    {
        byte[] buffer = new byte[length];
        stream.ReadExactly(buffer);

        return buffer;
    }

    public byte ReadByte()
    {
        int value = stream.ReadByte();

        if (value < 0)
        {
            throw new InvalidDataException($"Unexpected end of data at offset {stream.Position}.");
        }

        return (byte)value;
    }

    public int ReadInt32()
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        stream.ReadExactly(buffer);

        return BinaryPrimitives.ReadInt32LittleEndian(buffer);
    }

    public void RemapScenarioIds(Db.ScenarioId target)
    {
        targetScenarioNumber = target.Value[..IdScenarioNumberLength];
    }

    public string ReadFixedString(int length)
    {
        return encoding.MakeNullTerminatedString(ReadBytes(length));
    }

    private Db.Id Remap(Db.Id id)
    {
        if (targetScenarioNumber is null || !IsScenarioId(id))
        {
            return id;
        }

        return new Db.Id(string.Concat(targetScenarioNumber, id.Value.AsSpan(IdScenarioNumberLength)));
    }

    private static bool IsScenarioId(Db.Id id)
    {
        return id.Category == Db.IdCategory.Scenario && id.CategoryIndex >= 0 && id.TypeCode.Length > 0;
    }

    public bool HasField(string name)
    {
        if (name.Length > MaxFieldNameLength)
        {
            throw new InvalidDataException($"Field name '{name}' is longer than {MaxFieldNameLength} characters.");
        }

        long offset = stream.Position;
        Span<byte> buffer = stackalloc byte[MaxFieldNameLength];
        int read = stream.ReadAtLeast(buffer[..name.Length], name.Length, throwOnEndOfStream: false);
        stream.Seek(offset, SeekOrigin.Begin);

        if (read < name.Length)
        {
            return false;
        }

        for (int i = 0; i < name.Length; i++)
        {
            if (buffer[i] != (byte)name[i])
            {
                return false;
            }
        }

        return true;
    }

    public void ExpectName(string name)
    {
        long offset = stream.Position;
        string actual = encoding.GetString(ReadBytes(name.Length));

        if (actual != name)
        {
            throw new InvalidDataException($"Expected record '{name}' at offset {offset} but found '{actual}'.");
        }
    }

    public int ReadUnnamedCount(int nameLength)
    {
        Skip(nameLength);

        return ReadInt32();
    }

    public int ReadInt(string name)
    {
        ExpectName(name);

        return ReadInt32();
    }

    public int? TryReadInt(string name)
    {
        if (!HasField(name))
        {
            return null;
        }

        return ReadInt(name);
    }

    public bool ReadBool(string name)
    {
        ExpectName(name);

        return ReadByte() != 0;
    }

    public bool? TryReadBool(string name)
    {
        if (!HasField(name))
        {
            return null;
        }

        return ReadBool(name);
    }

    public string ReadString(string name)
    {
        ExpectName(name);

        return ReadFixedString(ReadInt32());
    }

    public string? TryReadString(string name)
    {
        if (!HasField(name))
        {
            return null;
        }

        return ReadString(name);
    }

    public string? ReadOptionalString(string name)
    {
        string value = ReadString(name);

        if (value.Length <= 0)
        {
            return null;
        }

        return value;
    }

    public string? TryReadOptionalString(string name)
    {
        if (!HasField(name))
        {
            return null;
        }

        return ReadOptionalString(name);
    }

    public Db.Id ReadId(string name)
    {
        return Remap(new Db.Id(ReadString(name)));
    }

    public Db.Id? TryReadId(string name)
    {
        if (!HasField(name))
        {
            return null;
        }

        return ReadId(name);
    }

    public Db.Id? ReadOptionalId(string name)
    {
        string value = ReadString(name);

        if (value.Length <= 0 || string.Equals(value, EmptyId, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Remap(new Db.Id(value));
    }

    public Db.Id? TryReadOptionalId(string name)
    {
        if (!HasField(name))
        {
            return null;
        }

        return ReadOptionalId(name);
    }

    public Db.ResourceSet ReadBank(string name)
    {
        return Db.ResourceSet.Parse(ReadString(name));
    }

    public Db.ResourceSet? TryReadBank(string name)
    {
        if (!HasField(name))
        {
            return null;
        }

        return ReadBank(name);
    }

    public (int X, int Y) ReadPosition()
    {
        int x = ReadInt("POS_X");
        int y = ReadInt("POS_Y");

        return (x, y);
    }

    public (int X, int Y) ReadSize()
    {
        int x = ReadInt("SIZE_X");
        int y = ReadInt("SIZE_Y");

        return (x, y);
    }
}
