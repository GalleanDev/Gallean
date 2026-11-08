namespace Gallean.Client;

internal static class LeaderFormatter
{
    public static string FormatLevel(int level)
    {
        return Tr("Lv. {0}", level);
    }

    public static string FormatBattlesWon(int battlesWon)
    {
        if (battlesWon == 1)
        {
            return Tr("1 battle won");
        }
        else
        {
            return Tr("{0} battles won", battlesWon);
        }
    }
}
