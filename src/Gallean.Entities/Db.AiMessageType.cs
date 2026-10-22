namespace Gallean.Entities;

public static partial class Db
{
    public enum AiMessageType
    {
        PlayerAcceptsAll = 0,
        PlayerRefusedAll = 1,
        AiBreakAlliance = 2,
        PlayerBreakAll = 3,
        AiOffersAlliance = 4,
        PlayerGiveGold = 5,
        AiGiveGold = 6,
        TradeAccepted = 7,
        TradeRefused = 8,
        HateMessage = 9,
        NoGold = 10,
        PlayerGiveGoldN = 11
    }
}
