using System.Buffers.Binary;

namespace Extractor;

internal static class CampaignFileLoader
{
    private const int HeaderLength = 340;
    private const int HeaderNameOffset = 4;
    private const int HeaderAuthorOffset = 293;
    private const int HeaderPlayRaceOffset = 328;
    private const int HeaderSavedGameOffset = 336;
    private const string HeaderFileName = "HEADER.CPN";
    private const string OrderFileName = "ORDER.CPN";
    private const string ScenarioFileExtension = ".SG";

    public static CampaignFile Load(string path, ScenarioTextEncoding encoding)
    {
        return Load(File.ReadAllBytes(path), encoding);
    }

    public static CampaignFile Load(byte[] data, ScenarioTextEncoding encoding)
    {
        CampaignFileContent content = Read(MqdbArchive.Load(data, encoding), encoding);
        List<CampaignFileScenario> scenarios = [];

        foreach (CampaignFileEntry entry in content.Scenarios)
        {
            using MemoryStream stream = new(entry.Data, writable: false);

            scenarios.Add(new CampaignFileScenario(entry.FileName, ScenarioFileLoader.Load(stream, encoding)));
        }

        return new CampaignFile(content.Header, scenarios);
    }

    public static CampaignFileContent Read(string path, ScenarioTextEncoding encoding)
    {
        return Read(MqdbArchive.Load(File.ReadAllBytes(path), encoding), encoding);
    }

    private static CampaignFileContent Read(MqdbArchive archive, ScenarioTextEncoding encoding)
    {
        CampaignFileHeader header = ReadHeader(archive, encoding);
        List<CampaignFileEntry> scenarios = [];

        foreach (string fileName in ReadScenarioOrder(archive, encoding))
        {
            scenarios.Add(new CampaignFileEntry(fileName, archive.Read(fileName)));
        }

        return new CampaignFileContent(header, scenarios);
    }

    private static CampaignFileHeader ReadHeader(MqdbArchive archive, ScenarioTextEncoding encoding)
    {
        byte[] header = archive.Read(HeaderFileName);

        if (header.Length < HeaderLength)
        {
            throw new InvalidDataException($"'{HeaderFileName}' is {header.Length} bytes long, expected {HeaderLength}.");
        }

        return new CampaignFileHeader(
            encoding.MakeNullTerminatedString(header.AsSpan(HeaderNameOffset)),
            encoding.MakeNullTerminatedString(header.AsSpan(HeaderAuthorOffset)),
            IdSerializer.Deserialize(BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(HeaderPlayRaceOffset))),
            header[HeaderSavedGameOffset] != 0);
    }

    private static IReadOnlyList<string> ReadScenarioOrder(MqdbArchive archive, ScenarioTextEncoding encoding)
    {
        byte[] order = archive.Read(OrderFileName);
        int count = BinaryPrimitives.ReadInt32LittleEndian(order);
        int position = sizeof(int);

        Dictionary<string, string> scenarioFiles = new(StringComparer.OrdinalIgnoreCase);

        foreach (string fileName in archive.FileNames)
        {
            if (fileName.EndsWith(ScenarioFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                scenarioFiles[fileName] = fileName;
            }
        }

        List<string> result = [];

        for (int i = 0; i < count && position < order.Length; i++)
        {
            string name = encoding.MakeNullTerminatedString(order.AsSpan(position));
            position += name.Length + 1;

            if (!scenarioFiles.TryGetValue(name, out string? fileName))
            {
                throw new InvalidDataException($"Cannot find '{name}' in the archive.");
            }

            result.Add(fileName);
        }

        return result;
    }
}
