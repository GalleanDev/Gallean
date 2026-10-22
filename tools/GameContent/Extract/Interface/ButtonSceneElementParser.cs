namespace GameContent;

internal static class ButtonSceneElementParser
{
    public static SceneElement Parse(string line, int offsetX, int offsetY)
    {
        string[] elements = line.Split(',');

        if (elements.Length < 11)
        {
            throw new ResourceException($"Could not parse line into button: '{line}'.");
        }

        return new ButtonSceneElement
        {
            Name = elements[0],
            Position = elements.ParseBounds(1, offsetX, offsetY),
            ActiveStateImageName = elements[5].ParseImageName(),
            HoverStateImageName = elements[6].ParseImageName(),
            PressedStateImageName = elements[7].ParseImageName(),
            DisabledStateImageName = elements[8].ParseImageName(),
            ToolTipTextId = elements[9].ParseTextId(),
            IsRepeat = elements[10].ParseBoolean(),
            HotKeys = [.. elements.Skip(11).Select(int.Parse)]
        };
    }
}
