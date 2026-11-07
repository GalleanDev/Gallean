using Gallean.Engine;

namespace Gallean.Client;

internal sealed class ThiefActionResultScreenViewModel : AbstractScreenViewModel
{
    private ThiefActionResultScreenViewModel(string title, string text, WorldThiefActionType type, bool isSuccess)
    {
        IsSuccess = isSuccess;
        Title = title;
        Text = text;
        ImageKey = new AssetSpriteKey.ThiefActionImage(type, IsLarge: true);
    }

    public bool IsSuccess { get; }
    public string Title { get; }
    public string Text { get; }
    public AssetSpriteKey.ThiefActionImage ImageKey { get; }

    public static ThiefActionResultScreenViewModel CreateMineResult(
        WorldThiefActionType type,
        bool isSuccess)
    {
        string title = FormatTitle(isSuccess);
        string text = FormatMineText(type, isSuccess);

        return new ThiefActionResultScreenViewModel(title, text, type, isSuccess);
    }

    public static ThiefActionResultScreenViewModel CreateTheirResult(
        WorldThiefActionType type,
        WorldUnitGroup target,
        bool isSuccess,
        string theirLordName)
    {
        string title = FormatTitle(isSuccess);
        string text = FormatTheirText(type, target, isSuccess, theirLordName);

        return new ThiefActionResultScreenViewModel(title, text, type, isSuccess);
    }

    public override bool CanContinue => true;

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

    private static string FormatMineText(WorldThiefActionType type, bool isSuccess)
    {
        if (isSuccess)
        {
            return FormatMineSuccess(type);
        }
        else
        {
            return Tr("Your thief has been captured and executed!");
        }
    }

    private static string FormatMineSuccess(WorldThiefActionType type)
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

    private static string FormatTheirText(
        WorldThiefActionType type,
        WorldUnitGroup target,
        bool isSuccess,
        string theirLordName)
    {
        if (isSuccess)
        {
            return FormatTheirSuccess(type, target, theirLordName);
        }
        else
        {
            return Tr("{0}'s thief has been captured and executed!", theirLordName);
        }
    }

    private static string FormatTheirSuccess(WorldThiefActionType type, WorldUnitGroup target, string theirLordName)
    {
        return type switch
        {
            WorldThiefActionType.PoisonStack => Tr("{0} has poisoned your party.", theirLordName),
            WorldThiefActionType.Spy => FormatTheirSpy(target, theirLordName),
            WorldThiefActionType.StealItem => Tr("{0} has stolen an item from you.", theirLordName),
            WorldThiefActionType.Assassinate => Tr("{0} has assassinated one of your soldiers.", theirLordName),
            WorldThiefActionType.CounterfeitOrders => Tr("{0} has disorganized your troops.", theirLordName),
            WorldThiefActionType.Duel => Tr("{0} has provoked your leader into a duel.", theirLordName),
            WorldThiefActionType.PoisonCity => Tr("{0} has poisoned your city.", theirLordName),
            WorldThiefActionType.StealSpell => Tr("{0} has stolen a spell from you.", theirLordName),
            WorldThiefActionType.Bribe => Tr("{0} has bribed your city.", theirLordName),
            WorldThiefActionType.StealGold => Tr("{0} has stolen gold from you.", theirLordName),
            WorldThiefActionType.RiotCity => Tr("A riot has been provoked inside the city!"),
            WorldThiefActionType.StealMerchant or WorldThiefActionType.StealMage or WorldThiefActionType.SpyRuin =>
                throw new ArgumentOutOfRangeException(nameof(type), type, "A thief action on a site or a ruin has no target player.")
        };
    }

    private static string FormatTheirSpy(WorldUnitGroup target, string theirLordName)
    {
        if (target is WorldUnitGroup.Fortification)
        {
            return Tr("{0} has placed a spy in your city.", theirLordName);
        }
        else
        {
            return Tr("{0} has placed a spy in your army.", theirLordName);
        }
    }
}
