namespace GameContent;

public sealed class CapitalSceneExtractor
{
    private const string LayerHeader = "LAYER_";

    private readonly IReadOnlyDictionary<Db.Id, CapitalSceneLayer> buildings;
    private readonly IReadOnlyDictionary<string, CapitalScene> scenes;

    private CapitalSceneExtractor(
        IReadOnlyDictionary<string, CapitalScene> scenes,
        IReadOnlyDictionary<Db.Id, CapitalSceneLayer> buildings)
    {
        this.scenes = scenes;
        this.buildings = buildings;
    }

    public static CapitalSceneExtractor Load(string path)
    {
        using StreamReader stream = new(path);

        Dictionary<string, List<CapitalSceneLayer>> layersByRace = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<Db.Id, CapitalSceneLayer> buildings = [];
        List<CapitalSceneLayer>? currentLayers = null;

        while (stream.ReadLine() is { } line)
        {
            string? race = ParseRace(line);

            if (race is null)
            {
                if (currentLayers is not null && line.StartsWith(LayerHeader, StringComparison.Ordinal))
                {
                    CapitalSceneLayer layer = ParseCapitalSceneLayer(line);
                    currentLayers.Add(layer);

                    if (layer.BuildingId is not null)
                    {
                        buildings.Add(layer.BuildingId.Value, layer);
                    }
                }
            }
            else
            {
                currentLayers = [];
                layersByRace.Add(race, currentLayers);
            }
        }

        Dictionary<string, CapitalScene> scenes = new(layersByRace.Count, StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, List<CapitalSceneLayer>> pair in layersByRace)
        {
            scenes.Add(pair.Key, new CapitalScene([.. pair.Value.OrderBy(layer => layer.Order)]));
        }

        return new CapitalSceneExtractor(scenes, buildings);
    }

    public IReadOnlyDictionary<Db.Id, CapitalSceneLayer> GetAllCapitalBuildings()
    {
        return buildings;
    }

    public IReadOnlyDictionary<string, CapitalScene> GetAllCapitalScenes()
    {
        return scenes;
    }

    private static string? ParseRace(string line)
    {
        if (!line.StartsWith('[') || !line.EndsWith(']'))
        {
            return null;
        }

        return line[1..^1];
    }

    private static CapitalSceneLayer ParseCapitalSceneLayer(string line)
    {
        int separator = line.IndexOf('=');

        if (separator < 0)
        {
            throw new ResourceException($"Invalid capital scene layer '{line}'.");
        }

        if (!int.TryParse(line[LayerHeader.Length..separator], out int order))
        {
            throw new ResourceException($"Invalid capital scene layer '{line}'.");
        }

        string[] fields = line[(separator + 1)..].Split(',');

        if (fields.Length < 5 || string.IsNullOrWhiteSpace(fields[1]))
        {
            throw new ResourceException($"Invalid capital scene layer '{line}'.");
        }

        Db.Id? buildingId = ParseBuildingId(fields[0]);

        return new CapitalSceneLayer(
            order,
            buildingId,
            fields[1],
            int.Parse(fields[3]),
            int.Parse(fields[4]));
    }

    private static Db.Id? ParseBuildingId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        Db.Id buildingId = new(value);

        if (buildingId.Type != Db.IdType.Building)
        {
            throw new ResourceException($"Invalid capital building ID '{buildingId}'.");
        }

        return buildingId;
    }
}
