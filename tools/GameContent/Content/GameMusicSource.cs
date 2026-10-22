namespace GameContent;

internal sealed class GameMusicSource(string musicRoot)
{
    private readonly IReadOnlyDictionary<string, string> fileNames = BuildFileNames(musicRoot);

    public static GameMusicSource? TryLoad(string musicRoot)
    {
        if (Directory.Exists(musicRoot))
        {
            return new GameMusicSource(musicRoot);
        }
        else
        {
            return null;
        }
    }

    public IReadOnlyList<AssetSound>? GetMusic(IReadOnlyList<string> names)
    {
        List<AssetSound> variants = [];

        foreach (string name in names)
        {
            if (fileNames.TryGetValue(name, out string? path))
            {
                variants.Add(new AssetSound(new AssetSoundMetadata(), AssetStream.FromFile(path)));
            }
        }

        if (variants.Count > 0)
        {
            return variants;
        }
        else
        {
            return null;
        }
    }

    private static IReadOnlyDictionary<string, string> BuildFileNames(string musicRoot)
    {
        Dictionary<string, string> pathsByName = new(StringComparer.OrdinalIgnoreCase);

        foreach (string path in Directory.EnumerateFiles(musicRoot))
        {
            pathsByName[Path.GetFileName(path)] = path;
        }

        return pathsByName;
    }
}
