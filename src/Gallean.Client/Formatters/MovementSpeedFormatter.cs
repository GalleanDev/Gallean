using Gallean.Configuration;

namespace Gallean.Client;

internal static class MovementSpeedFormatter
{
    public static string Format(MovementSpeed speed)
    {
        return speed switch
        {
            MovementSpeed.Normal => Tr("Normal"),
            MovementSpeed.Fast => Tr("Fast")
        };
    }
}
