namespace Gallean.Engine.Scenarios;

internal sealed class MqdbArchive
{
    private readonly record struct Record(int Size, long Offset);

    private const string ArchiveSignature = "MQDB";
    private const string RecordSignature = "MQRC";
    private const int ArchiveVersionOffset = 8;
    private const int ArchiveVersion = 9;
    private const int RecordTableOffset = 28;
    private const int FileTableRecordId = 2;
    private const int FileNameLength = 256;

    private readonly ScenarioRecordReader reader;
    private readonly Dictionary<string, Record> filesByName;

    private MqdbArchive(ScenarioRecordReader reader, Dictionary<string, Record> filesByName)
    {
        this.reader = reader;
        this.filesByName = filesByName;
    }

    public IReadOnlyCollection<string> FileNames => filesByName.Keys;

    public static MqdbArchive Load(Stream stream, ScenarioTextEncoding encoding)
    {
        ScenarioRecordReader reader = new(stream, encoding);

        reader.Seek(0);

        if (reader.ReadFixedString(ArchiveSignature.Length) != ArchiveSignature)
        {
            throw new InvalidDataException($"Archive does not start with '{ArchiveSignature}'.");
        }

        reader.Seek(ArchiveVersionOffset);

        if (reader.ReadInt32() != ArchiveVersion)
        {
            throw new InvalidDataException($"Archive is not at version {ArchiveVersion}.");
        }

        Dictionary<int, Record> recordsById = ReadRecords(reader);

        if (!recordsById.TryGetValue(FileTableRecordId, out Record fileTable))
        {
            throw new InvalidDataException($"Archive has no file table record '{FileTableRecordId}'.");
        }

        return new MqdbArchive(reader, ReadFileTable(reader, fileTable, recordsById));
    }

    public byte[] Read(string name)
    {
        if (!filesByName.TryGetValue(name, out Record record))
        {
            throw new InvalidDataException($"Archive has no file named '{name}'.");
        }

        reader.Seek(record.Offset);

        return reader.ReadBytes(record.Size);
    }

    private static Dictionary<int, Record> ReadRecords(ScenarioRecordReader reader)
    {
        Dictionary<int, Record> result = [];
        long position = RecordTableOffset;

        while (position < reader.Length - 1)
        {
            reader.Seek(position);

            if (reader.ReadFixedString(RecordSignature.Length) != RecordSignature)
            {
                throw new InvalidDataException($"Expected '{RecordSignature}' at offset {position}.");
            }

            reader.Skip(sizeof(int));

            int id = reader.ReadInt32();
            int size = reader.ReadInt32();
            int storedSize = reader.ReadInt32();

            reader.Skip(2 * sizeof(int));

            result[id] = new Record(size, reader.Position);
            position = reader.Position + storedSize;
        }

        return result;
    }

    private static Dictionary<string, Record> ReadFileTable(
        ScenarioRecordReader reader,
        Record fileTable,
        Dictionary<int, Record> recordsById)
    {
        Dictionary<string, Record> result = [with(StringComparer.OrdinalIgnoreCase)];

        reader.Seek(fileTable.Offset);

        int count = reader.ReadInt32();

        for (int i = 0; i < count; i++)
        {
            string name = reader.ReadFixedString(FileNameLength);
            int id = reader.ReadInt32();

            if (recordsById.TryGetValue(id, out Record record))
            {
                result[name] = record;
            }
        }

        return result;
    }
}
