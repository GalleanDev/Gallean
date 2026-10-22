using System.Buffers.Binary;

namespace Extractor;

internal sealed class MqdbArchive
{
    private readonly record struct Record(int Size, int Offset);

    private const string ArchiveSignature = "MQDB";
    private const string RecordSignature = "MQRC";
    private const int ArchiveVersionOffset = 8;
    private const int ArchiveVersion = 9;
    private const int RecordTableOffset = 28;
    private const int FileTableRecordId = 2;
    private const int FileNameLength = 256;

    private readonly byte[] data;
    private readonly Dictionary<string, Record> filesByName;

    private MqdbArchive(byte[] data, Dictionary<string, Record> filesByName)
    {
        this.data = data;
        this.filesByName = filesByName;
    }

    public IReadOnlyCollection<string> FileNames => filesByName.Keys;

    public static MqdbArchive Load(byte[] data, ScenarioTextEncoding encoding)
    {
        if (encoding.GetString(data.AsSpan(0, ArchiveSignature.Length)) != ArchiveSignature)
        {
            throw new InvalidDataException($"Archive does not start with '{ArchiveSignature}'.");
        }

        if (BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(ArchiveVersionOffset)) != ArchiveVersion)
        {
            throw new InvalidDataException($"Archive is not at version {ArchiveVersion}.");
        }

        Dictionary<int, Record> recordsById = ReadRecords(data, encoding);

        if (!recordsById.TryGetValue(FileTableRecordId, out Record fileTable))
        {
            throw new InvalidDataException($"Archive has no file table record '{FileTableRecordId}'.");
        }

        return new MqdbArchive(data, ReadFileTable(data, fileTable, recordsById, encoding));
    }

    public byte[] Read(string name)
    {
        if (!filesByName.TryGetValue(name, out Record record))
        {
            throw new InvalidDataException($"Archive has no file named '{name}'.");
        }

        return data.AsSpan(record.Offset, record.Size).ToArray();
    }

    private static Dictionary<int, Record> ReadRecords(byte[] data, ScenarioTextEncoding encoding)
    {
        Dictionary<int, Record> result = [];
        int position = RecordTableOffset;

        while (position < data.Length - 1)
        {
            if (encoding.GetString(data.AsSpan(position, RecordSignature.Length)) != RecordSignature)
            {
                throw new InvalidDataException($"Expected '{RecordSignature}' at offset {position}.");
            }

            position += RecordSignature.Length + sizeof(int);

            int id = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(position));
            int size = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(position + 4));
            int storedSize = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(position + 8));
            position += 5 * sizeof(int);

            result[id] = new Record(size, position);
            position += storedSize;
        }

        return result;
    }

    private static Dictionary<string, Record> ReadFileTable(
        byte[] data,
        Record fileTable,
        Dictionary<int, Record> recordsById,
        ScenarioTextEncoding encoding)
    {
        Dictionary<string, Record> result = new(StringComparer.OrdinalIgnoreCase);
        int position = fileTable.Offset;
        int count = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(position));
        position += sizeof(int);

        for (int i = 0; i < count; i++)
        {
            string name = encoding.MakeNullTerminatedString(data.AsSpan(position, FileNameLength));
            int id = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(position + FileNameLength));
            position += FileNameLength + sizeof(int);

            if (recordsById.TryGetValue(id, out Record record))
            {
                result[name] = record;
            }
        }

        return result;
    }
}
