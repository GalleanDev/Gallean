using System.Text;

namespace Extractor.Tests;

public static class TestCampaignArchiveBuilder
{
    private const int FileTableRecordId = 2;
    private const int FileNameLength = 256;
    private const int ArchiveHeaderLength = 28;
    private const int ArchiveVersionOffset = 8;
    private const int ArchiveVersion = 9;
    private const int HeaderLength = 340;
    private const int HeaderNameOffset = 4;
    private const int HeaderAuthorOffset = 293;
    private const int HeaderPlayRaceOffset = 328;
    private const int HeaderSavedGameOffset = 336;

    public static byte[] Build(
        string name,
        string author,
        Db.Id playRaceId,
        IReadOnlyList<(string FileName, string ScenarioPath)> scenarios)
    {
        List<(string Name, byte[] Data)> files =
        [
            ("HEADER.CPN", BuildHeader(name, author, playRaceId, hasSavedGame: false)),
            ("ORDER.CPN", BuildOrder([.. scenarios.Select(scenario => scenario.FileName)]))
        ];

        foreach ((string fileName, string scenarioPath) in scenarios)
        {
            files.Add((fileName, File.ReadAllBytes(scenarioPath)));
        }

        return BuildArchive(files);
    }

    public static byte[] BuildArchive(IReadOnlyList<(string Name, byte[] Data)> files)
    {
        MemoryStream fileTable = new();
        fileTable.Write(BitConverter.GetBytes(files.Count));

        for (int i = 0; i < files.Count; i++)
        {
            byte[] name = new byte[FileNameLength];
            Encoding.ASCII.GetBytes(files[i].Name).CopyTo(name, 0);
            fileTable.Write(name);
            fileTable.Write(BitConverter.GetBytes(FileTableRecordId + 1 + i));
        }

        byte[] header = new byte[ArchiveHeaderLength];
        "MQDB"u8.ToArray().CopyTo(header, 0);
        BitConverter.GetBytes(ArchiveVersion).CopyTo(header, ArchiveVersionOffset);

        MemoryStream result = new();
        result.Write(header);

        WriteRecord(result, FileTableRecordId, fileTable.ToArray());

        for (int i = 0; i < files.Count; i++)
        {
            WriteRecord(result, FileTableRecordId + 1 + i, files[i].Data);
        }

        return result.ToArray();
    }

    public static byte[] BuildHeader(string name, string author, Db.Id playRaceId, bool hasSavedGame)
    {
        byte[] result = new byte[HeaderLength];
        BitConverter.GetBytes(100).CopyTo(result, 0);
        Encoding.ASCII.GetBytes(name).CopyTo(result, HeaderNameOffset);
        Encoding.ASCII.GetBytes(author).CopyTo(result, HeaderAuthorOffset);
        BitConverter.GetBytes(IdSerializer.Serialize(playRaceId)).CopyTo(result, HeaderPlayRaceOffset);
        result[HeaderSavedGameOffset] = hasSavedGame ? (byte)1 : (byte)0;

        return result;
    }

    public static byte[] BuildOrder(IReadOnlyList<string> fileNames)
    {
        MemoryStream result = new();
        result.Write(BitConverter.GetBytes(fileNames.Count));

        foreach (string fileName in fileNames)
        {
            result.Write(Encoding.ASCII.GetBytes(fileName));
            result.WriteByte(0);
        }

        return result.ToArray();
    }

    private static void WriteRecord(Stream stream, int id, byte[] data)
    {
        stream.Write("MQRC"u8);
        stream.Write(new byte[4]);
        stream.Write(BitConverter.GetBytes(id));
        stream.Write(BitConverter.GetBytes(data.Length));
        stream.Write(BitConverter.GetBytes(data.Length));
        stream.Write(BitConverter.GetBytes(1));
        stream.Write(BitConverter.GetBytes(0));
        stream.Write(data);
    }
}
