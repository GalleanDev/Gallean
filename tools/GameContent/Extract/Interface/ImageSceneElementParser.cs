namespace GameContent;

internal static class ImageSceneElementParser
{
    public static SceneElement Parse(string line, int offsetX, int offsetY)
    {
        string[] elements = line.Split(',');

        if (elements.Length < 7)
        {
            throw new ResourceException($"Could not parse line into image: '{line}'.");
        }

        return new ImageSceneElement
        {
            Name = elements[0],
            Position = elements.ParseBounds(1, offsetX, offsetY),
            ImageName = elements[5].ParseImageName(),
            ToolTipTextId = elements[6].ParseTextId()
        };
    }
}
