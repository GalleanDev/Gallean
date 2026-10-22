namespace GameContent;

public sealed class SoundsExtractor
{
    private const int FileTableRecordId = 1;

    private readonly MqdbResourceArchive archive;

    private SoundsExtractor(MqdbResourceArchive archive)
    {
        this.archive = archive;
    }

    public static SoundsExtractor Load(string path)
    {
        return new SoundsExtractor(MqdbResourceArchive.Load(path, FileTableRecordId, MqdbFileTableLayout.IdName));
    }

    public IReadOnlyList<string> GetAllFilesNames()
    {
        return archive.FileNames;
    }

    public byte[]? GetSound(string soundName)
    {
        MqdbResourceFile? soundFile = archive.TryGetFile(soundName);

        if (soundFile is not null)
        {
            return archive.ReadFile(soundFile);
        }
        else
        {
            return null;
        }
    }
}
