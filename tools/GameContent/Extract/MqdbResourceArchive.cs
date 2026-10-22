namespace GameContent;

internal sealed class MqdbResourceArchive
{
    private const string ArchiveHeader = "MQDB";
    private const string RecordHeader = "MQRC";
    private const int RecordTableOffset = 28;
    private const int NamedEntryNameLength = 256;
    private const int SoundEntryNameLength = 20;
    private const int SoundEntryLength = 24;

    private readonly IReadOnlyDictionary<int, MqdbResourceFile> filesById;
    private readonly IReadOnlyDictionary<string, MqdbResourceFile> filesByName;
    private readonly string path;

    private MqdbResourceArchive(string path, IReadOnlyList<MqdbResourceFile> files)
    {
        this.path = path;
        filesById = files.ToDictionary(file => file.Id, file => file);
        filesByName = files.ToDictionary(file => file.Name, file => file, StringComparer.OrdinalIgnoreCase);
        FileNames = [.. files.Select(file => file.Name)];
    }

    public IReadOnlyList<string> FileNames { get; }

    public static MqdbResourceArchive Load(string path, int fileTableRecordId, MqdbFileTableLayout layout)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read);

        string header = stream.ReadAsciiString(ArchiveHeader.Length);
        if (header != ArchiveHeader)
        {
            throw new ResourceException("Unexpected file header.");
        }

        IReadOnlyDictionary<int, MqdbResourceRecord> records = ReadRecords(stream);
        if (!records.TryGetValue(fileTableRecordId, out MqdbResourceRecord? fileTable))
        {
            throw new ResourceNotFoundException($"Cannot find resource file list record with ID '{fileTableRecordId}'.");
        }

        stream.Seek(fileTable.Offset, SeekOrigin.Begin);

        IReadOnlyList<MqdbResourceFile> files = layout switch
        {
            MqdbFileTableLayout.NameId => ReadNamedFileTable(stream, records),
            MqdbFileTableLayout.IdName => ReadSoundFileTable(stream, fileTable, records)
        };

        return new MqdbResourceArchive(path, files);
    }

    public MqdbResourceFile GetFile(int id)
    {
        return TryGetFile(id) ?? throw new ResourceNotFoundException($"Cannot find file with ID '{id}'.");
    }

    public MqdbResourceFile? TryGetFile(int id)
    {
        return filesById.GetValueOrDefault(id);
    }

    public MqdbResourceFile GetFile(string fileName)
    {
        return TryGetFile(fileName) ?? throw new ResourceNotFoundException($"Cannot find file with name '{fileName}'.");
    }

    public MqdbResourceFile? TryGetFile(string fileName)
    {
        return filesByName.GetValueOrDefault(fileName);
    }

    public FileStream OpenArchive()
    {
        return new FileStream(path, FileMode.Open, FileAccess.Read);
    }

    public byte[] ReadFile(MqdbResourceFile file)
    {
        byte[] content = new byte[file.Size];

        using FileStream stream = OpenArchive();
        stream.Seek(file.Offset, SeekOrigin.Begin);
        stream.ReadExactly(content, 0, file.Size);

        return content;
    }

    private static IReadOnlyDictionary<int, MqdbResourceRecord> ReadRecords(Stream stream)
    {
        Dictionary<int, MqdbResourceRecord> records = [];
        stream.Seek(RecordTableOffset, SeekOrigin.Begin);

        while (stream.Position < stream.Length - 1)
        {
            string header = stream.ReadAsciiString(RecordHeader.Length);
            if (header != RecordHeader)
            {
                throw new InvalidDataException("Unexpected record header.");
            }

            stream.Skip(4);

            int id = stream.ReadInt32LittleEndian();
            int size = stream.ReadInt32LittleEndian();
            int storedSize = stream.ReadInt32LittleEndian();
            stream.Skip(8);

            records[id] = new MqdbResourceRecord(id, size, stream.Position);

            stream.Skip(storedSize);
        }

        return records;
    }

    private static IReadOnlyList<MqdbResourceFile> ReadNamedFileTable(
        Stream stream,
        IReadOnlyDictionary<int, MqdbResourceRecord> records)
    {
        int filesCount = stream.ReadInt32LittleEndian();
        List<MqdbResourceFile> files = new(filesCount);

        for (int i = 0; i < filesCount; i++)
        {
            string fileName = stream.ReadAsciiString(NamedEntryNameLength);
            int id = stream.ReadInt32LittleEndian();

            MqdbResourceRecord record = records[id];
            files.Add(new MqdbResourceFile(id, fileName, record.Size, record.Offset));
        }

        return files;
    }

    private static IReadOnlyList<MqdbResourceFile> ReadSoundFileTable(
        Stream stream,
        MqdbResourceRecord fileTable,
        IReadOnlyDictionary<int, MqdbResourceRecord> records)
    {
        int filesCount = fileTable.Size / SoundEntryLength;

        stream.Skip(8);

        List<MqdbResourceFile> files = new(filesCount);

        for (int i = 0; i < filesCount; i++)
        {
            int id = stream.ReadInt32LittleEndian();
            string fileName = stream.ReadAsciiString(SoundEntryNameLength);

            MqdbResourceRecord record = records[id];
            files.Add(new MqdbResourceFile(id, fileName, record.Size, record.Offset));
        }

        return files;
    }
}
