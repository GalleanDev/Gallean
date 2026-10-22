using Gallean.Configuration;

namespace Gallean.Client;

internal static class BattleSpeedFormatter
{
    public static string Format(BattleSpeed speed)
    {
        return speed switch
        {
            BattleSpeed.Normal => Tr("Normal"),
            BattleSpeed.Fast => Tr("Fast")
        };
    }
}
