namespace Gallean.Client;

internal static class AutoSaveIntervalFormatter
{
    public static string Format(int interval)
    {
        return interval switch
        {
            0 => Tr("Never"),
            1 => Tr("Every turn"),
            _ => Tr("Every {0} turns", interval)
        };
    }
}
