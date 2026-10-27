using Gallean.Configuration;

namespace Gallean.Client;

internal static class UiSizeFormatter
{
    public static string Format(UiSize size)
    {
        return size switch
        {
            UiSize.Small => Tr("Small"),
            UiSize.Normal => Tr("Normal"),
            UiSize.Large => Tr("Large")
        };
    }
}
