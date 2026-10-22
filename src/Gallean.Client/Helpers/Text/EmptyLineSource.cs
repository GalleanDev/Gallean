using Avalonia.Media.TextFormatting;

namespace Gallean.Client;

internal sealed class EmptyLineSource(int lines) : ITextSource
{
    public TextRun? GetTextRun(int textSourceIndex)
    {
        if (textSourceIndex < lines)
        {
            return new TextEndOfLine();
        }
        else
        {
            return null;
        }
    }
}
