namespace Gallean.Client;

internal static class ThiefActionIconResolver
{
    public static IconMoniker Resolve(WorldThiefActionType type)
    {
        return type switch
        {
            WorldThiefActionType.PoisonStack => IconMoniker.ThiefPoisonStack,
            WorldThiefActionType.Spy => IconMoniker.ThiefSpy,
            WorldThiefActionType.StealItem => IconMoniker.ThiefStealItem,
            WorldThiefActionType.Assassinate => IconMoniker.ThiefAssassinate,
            WorldThiefActionType.CounterfeitOrders => IconMoniker.ThiefCounterfeitOrders,
            WorldThiefActionType.Duel => IconMoniker.ThiefDuel,
            WorldThiefActionType.PoisonCity => IconMoniker.ThiefPoisonCity,
            WorldThiefActionType.StealSpell => IconMoniker.ThiefStealSpell,
            WorldThiefActionType.Bribe => IconMoniker.ThiefBribe,
            WorldThiefActionType.StealGold => IconMoniker.ThiefStealGold,
            WorldThiefActionType.StealMerchant => IconMoniker.ThiefStealMerchant,
            WorldThiefActionType.StealMage => IconMoniker.ThiefStealMage,
            WorldThiefActionType.SpyRuin => IconMoniker.ThiefSpyRuin,
            WorldThiefActionType.RiotCity => IconMoniker.ThiefRiotCity
        };
    }
}
