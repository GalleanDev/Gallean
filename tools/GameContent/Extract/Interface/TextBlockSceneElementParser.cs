namespace GameContent;

internal static class TextBlockSceneElementParser
{
    public static SceneElement Parse(string line, int offsetX, int offsetY)
    {
        string[] elements = line.Split(',');

        if (elements.Length < 8)
        {
            throw new ResourceException($"Could not parse line into text block: '{line}'.");
        }

        return new TextBlockSceneElement
        {
            Name = elements[0],
            Position = elements.ParseBounds(1, offsetX, offsetY),
            TextStyle = elements[5].ParseTextStyle(),
            TextId = elements[6].ParseTextId(),
            ToolTipTextId = elements[7].ParseTextId()
        };
    }
}
