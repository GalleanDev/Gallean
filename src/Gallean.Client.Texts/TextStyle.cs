namespace Gallean.Client.Texts;

[Flags]
public enum TextStyle
{
    None = 0,
    Highlight = 1 << 0,
    Bold = 1 << 1,
    Italic = 1 << 2
}
