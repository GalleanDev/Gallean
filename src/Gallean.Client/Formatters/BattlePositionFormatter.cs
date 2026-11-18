using Gallean.Configuration;

namespace Gallean.Client;

internal static class BattlePositionFormatter
{
    public static string Format(BattlePosition position)
    {
        return position switch
        {
            BattlePosition.Left => Tr("Left"),
            BattlePosition.Right => Tr("Right")
        };
    }
}
