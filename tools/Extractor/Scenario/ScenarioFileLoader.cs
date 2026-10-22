namespace Extractor;

internal static class ScenarioFileLoader
{
    private const int ScenarioIdLength = 11;
    private const int SignatureLength = 10;
    private const int ScenarioHeaderPrefixLength = 16;
    private const int ScenarioHeaderIdOffset = 32;
    private const int ScenarioHeaderBodyOffset = 43;
    private const int ScenarioHeaderPreviewLengthOffset = 2645;
    private const int SaveHeaderVersionOffset = 16;
    private const int SaveHeaderIdOffset = 20;
    private const int ObjectCountNameLength = 10;
    private const int ObjectCountTypeOffset = 4;
    private const string ObjectCountTypeCode = "OB";

    private const int FirstSupportedSaveVersion = 31;
    private const int LastSupportedSaveVersion = 35;
    private const int DesignSaveVersion = 0;
    private const int DesignSaveHeaderSize = 2999;
    private const int SaveHeaderSize = 2743;

    public static ScenarioFile Load(string path, ScenarioTextEncoding encoding)
    {
        using FileStream stream = File.OpenRead(path);

        return Load(stream, encoding);
    }

    public static ScenarioFile Load(Stream stream, ScenarioTextEncoding encoding)
    {
        return LoadCore(new ScenarioRecordReader(stream, encoding), targetId: null);
    }

    public static ScenarioFile Load(Stream stream, ScenarioTextEncoding encoding, Db.ScenarioId targetId)
    {
        return LoadCore(new ScenarioRecordReader(stream, encoding), targetId);
    }

    private static ScenarioFile LoadCore(ScenarioRecordReader reader, Db.ScenarioId? targetId)
    {
        string signature = reader.ReadFixedString(SignatureLength);

        return signature switch
        {
            ScenarioFileFormat.ScenarioSignature => LoadScenario(reader, targetId),
            ScenarioFileFormat.SaveSignature => LoadSave(reader, targetId),
            _ => throw new InvalidDataException($"Unknown scenario file signature '{signature}'.")
        };
    }

    private static ScenarioFile LoadScenario(ScenarioRecordReader reader, Db.ScenarioId? targetId)
    {
        reader.Seek(ScenarioHeaderPrefixLength);

        int headerSize = reader.ReadInt32();

        reader.Seek(ScenarioHeaderIdOffset);
        Db.ScenarioId scenarioId = new(reader.ReadFixedString(ScenarioIdLength));

        return Parse(
            reader,
            ScenarioFileFormat.Scenario,
            scenarioId,
            targetId,
            ScenarioHeaderPrefixLength + headerSize + ReadPreviewLength(reader, headerSize));
    }

    private static int ReadPreviewLength(ScenarioRecordReader reader, int headerSize)
    {
        long offset = ScenarioHeaderBodyOffset + ScenarioHeaderPreviewLengthOffset;

        if (ScenarioHeaderPrefixLength + headerSize < offset + sizeof(int))
        {
            return 0;
        }

        reader.Seek(offset);

        return reader.ReadInt32();
    }

    private static ScenarioFile LoadSave(ScenarioRecordReader reader, Db.ScenarioId? targetId)
    {
        reader.Seek(SaveHeaderVersionOffset);
        int version = reader.ReadInt32();

        reader.Seek(SaveHeaderIdOffset);
        Db.ScenarioId scenarioId = new(reader.ReadFixedString(ScenarioIdLength));

        return Parse(
            reader,
            ScenarioFileFormat.Save(version),
            scenarioId,
            targetId,
            GetSaveHeaderSize(version));
    }

    private static int GetSaveHeaderSize(int version)
    {
        if (version == DesignSaveVersion)
        {
            return DesignSaveHeaderSize;
        }
        else if (version >= FirstSupportedSaveVersion && version <= LastSupportedSaveVersion)
        {
            return SaveHeaderSize;
        }
        else
        {
            throw new InvalidDataException($"Unsupported scenario save version '{version}'.");
        }
    }

    private static string GetObjectCountRecordName(Db.ScenarioId scenarioId)
    {
        string value = scenarioId.Value;

        if (value.Length != ObjectCountNameLength)
        {
            throw new InvalidDataException($"Scenario id '{value}' is not {ObjectCountNameLength} characters long.");
        }

        return string.Concat(
            value.AsSpan(0, ObjectCountTypeOffset),
            ObjectCountTypeCode,
            value.AsSpan(ObjectCountTypeOffset + ObjectCountTypeCode.Length));
    }

    private static int ReadObjectCount(ScenarioRecordReader reader, Db.ScenarioId scenarioId, long headerEnd)
    {
        reader.Seek(headerEnd);
        reader.ExpectName(GetObjectCountRecordName(scenarioId));

        return reader.ReadInt32();
    }

    private static ScenarioFile Parse(
        ScenarioRecordReader reader,
        ScenarioFileFormat format,
        Db.ScenarioId scenarioId,
        Db.ScenarioId? targetId,
        long headerEnd)
    {
        int objectCount = ReadObjectCount(reader, scenarioId, headerEnd);

        if (targetId is not null)
        {
            reader.RemapScenarioIds(targetId.Value);
            scenarioId = targetId.Value;
        }

        ScenarioFileParser parser = new(reader, format, scenarioId);
        ScenarioFileTables tables = parser.Parse(objectCount);

        if (reader.Position != reader.Length)
        {
            throw new InvalidDataException(
                $"Scenario '{scenarioId}' has {reader.Length - reader.Position} trailing bytes after its last object.");
        }

        return new ScenarioFile(scenarioId, format, tables);
    }
}
