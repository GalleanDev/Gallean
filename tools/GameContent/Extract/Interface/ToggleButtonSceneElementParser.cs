namespace GameContent;

internal static class ToggleButtonSceneElementParser
{
    public static SceneElement Parse(string line, int offsetX, int offsetY)
    {
        string[] elements = line.Split(',');

        if (elements.Length < 13)
        {
            throw new ResourceException($"Could not parse line into toggle button: '{line}'.");
        }

        return new ToggleButtonSceneElement
        {
            Name = elements[0],
            Position = elements.ParseBounds(1, offsetX, offsetY),
            ActiveStateImageName = elements[5].ParseImageName(),
            HoverStateImageName = elements[6].ParseImageName(),
            PressedStateImageName = elements[7].ParseImageName(),
            DisabledStateImageName = elements[8].ParseImageName(),
            CheckedActiveStateImageName = elements[9].ParseImageName(),
            CheckedHoverStateImageName = elements[10].ParseImageName(),
            CheckedPressedStateImageName = elements[11].ParseImageName(),
            ToolTipTextId = elements[12].ParseTextId(),
            HotKeys = [.. elements.Skip(13).Select(int.Parse)]
        };
    }
}
