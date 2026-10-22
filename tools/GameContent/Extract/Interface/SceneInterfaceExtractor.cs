namespace GameContent;

public sealed class SceneInterfaceExtractor
{
    private const string DialogHeader = "DIALOG";
    private const string DialogBegin = "BEGIN";
    private const string DialogEnd = "END";

    private const int ScreenWidth = 800;
    private const int ScreenHeight = 600;

    private readonly Dictionary<string, SceneInterface> dialogs;

    private SceneInterfaceExtractor(Dictionary<string, SceneInterface> dialogs)
    {
        this.dialogs = dialogs;
    }

    public static SceneInterfaceExtractor Load(string path)
    {
        using StreamReader stream = new(path);

        Dictionary<string, SceneInterface> dialogs = new(StringComparer.OrdinalIgnoreCase);

        while (stream.ReadLine() is { } line)
        {
            if (!string.IsNullOrEmpty(line))
            {
                if (line.StartsWith(DialogHeader))
                {
                    SceneInterface sceneInterface = ParseSceneInterface(line[(DialogHeader.Length + 1)..], stream);

                    dialogs.Add(sceneInterface.Name, sceneInterface);
                }
                else
                {
                    throw new ResourceException($"Invalid line '{line}', expected to start with '{DialogHeader}'.");
                }
            }
        }

        return new SceneInterfaceExtractor(dialogs);
    }

    public SceneInterface GetSceneInterface(string name)
    {
        if (dialogs.TryGetValue(name, out SceneInterface? sceneInterface))
        {
            return sceneInterface;
        }

        throw new ResourceNotFoundException($"Cannot find scene interface with name '{name}'.");
    }

    public IReadOnlyCollection<SceneInterface> GetAllSceneInterfaces()
    {
        return dialogs.Values;
    }

    private static SceneElement? ParseElement(string elementTypeName, string elementData, int offsetX, int offsetY)
    {
        return elementTypeName switch
        {
            "IMAGE" => ImageSceneElementParser.Parse(elementData, offsetX, offsetY),
            "BUTTON" => ButtonSceneElementParser.Parse(elementData, offsetX, offsetY),
            "TEXT" => TextBlockSceneElementParser.Parse(elementData, offsetX, offsetY),
            "TOGGLE" => ToggleButtonSceneElementParser.Parse(elementData, offsetX, offsetY),
            "TLBOX" => TextListBoxSceneElementParser.Parse(elementData, offsetX, offsetY),
            _ => null
        };
    }

    private static SceneInterface ParseSceneInterface(string dialogHeader, StreamReader stream)
    {
        string? begin = stream.ReadLine();
        if (begin != DialogBegin)
        {
            throw new ResourceException($"Invalid line '{begin}', expected '{DialogBegin}'.");
        }

        string[] headerData = dialogHeader.Split(',');
        Rectangle sceneBounds = headerData.ParseBounds(1);
        int offsetX = (ScreenWidth - sceneBounds.Width) / 2;
        int offsetY = (ScreenHeight - sceneBounds.Height) / 2;
        Rectangle bounds = sceneBounds with { X = sceneBounds.X + offsetX, Y = sceneBounds.Y + offsetY };

        List<SceneElement> elements = [];

        while (stream.ReadLine() is { } line)
        {
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            if (line == DialogEnd)
            {
                break;
            }

            line = line.TrimStart('\t');

            int elementTypeLength = line.IndexOf('\t');
            string elementTypeName = line[..elementTypeLength];
            SceneElement? element = ParseElement(elementTypeName, line[(elementTypeName.Length + 1)..], offsetX, offsetY);

            if (element is not null)
            {
                elements.Add(element);
            }
        }

        return new SceneInterface
        {
            Name = headerData[0],
            Bounds = bounds,
            BackgroundImageName = headerData[5].ParseImageName(),
            CursorImageName = headerData[6].ParseImageName(),
            CursorHotSpot = headerData.ParsePoint(7),
            Position = headerData.ParseBounds(9),
            IsSelfDrawn = headerData[13].ParseBoolean(),
            Elements = elements
        };
    }
}
