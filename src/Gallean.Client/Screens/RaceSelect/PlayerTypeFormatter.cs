namespace Gallean.Client;

internal static class PlayerTypeFormatter
{
    public static string Format(PlayerType type)
    {
        return type switch
        {
            PlayerType.Human => Tr("Human"),
            PlayerType.Computer => Tr("Computer")
        };
    }
}
