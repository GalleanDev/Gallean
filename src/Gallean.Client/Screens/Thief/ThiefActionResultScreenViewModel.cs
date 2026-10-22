namespace Gallean.Client;

internal sealed class ThiefActionResultScreenViewModel(WorldThiefActionType type, bool isSuccess) : AbstractScreenViewModel
{
    public string Title { get; } = FormatTitle(isSuccess);
    public string Text { get; } = FormatText(type, isSuccess);
    public AssetSpriteKey.ThiefActionImage ImageKey { get; } = new(type, IsLarge: true);

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }

    private static string FormatTitle(bool isSuccess)
    {
        if (isSuccess)
        {
            return Tr("Success!");
        }
        else
        {
            return Tr("Failure!");
        }
    }

    private static string FormatText(WorldThiefActionType type, bool isSuccess)
    {
        if (isSuccess)
        {
            return FormatSuccess(type);
        }
        else
        {
            return Tr("Your thief has been captured and executed!");
        }
    }

    private static string FormatSuccess(WorldThiefActionType type)
    {
        return type switch
        {
            WorldThiefActionType.PoisonStack => Tr("The enemy party has been poisoned!"),
            WorldThiefActionType.Spy => Tr("Your spy has infiltrated the enemy ranks. Right-click enemy parties and cities to reveal information about them!"),
            WorldThiefActionType.StealItem => Tr("An item has been stolen from the enemy party!"),
            WorldThiefActionType.Assassinate => Tr("An enemy unit has been assassinated!"),
            WorldThiefActionType.CounterfeitOrders => Tr("The false message worked, throwing the enemy troops into disarray!"),
            WorldThiefActionType.Duel => Tr("The enemy leader has been provoked into a duel!"),
            WorldThiefActionType.PoisonCity => Tr("The city has been poisoned!"),
            WorldThiefActionType.StealSpell => Tr("A spell has been stolen from the enemy!"),
            WorldThiefActionType.Bribe => Tr("The city has proclaimed its allegiance to you!"),
            WorldThiefActionType.StealGold => Tr("Gold coins have been stolen from the enemy!"),
            WorldThiefActionType.StealMerchant => Tr("An item has been stolen from the merchant!"),
            WorldThiefActionType.StealMage => Tr("A spell has been stolen from the mage tower!"),
            WorldThiefActionType.SpyRuin => Tr("Your thief has scouted the ruin. Right-click it to reveal information about it!"),
            WorldThiefActionType.RiotCity => Tr("Your thief has incited a riot in the city!")
        };
    }
}
