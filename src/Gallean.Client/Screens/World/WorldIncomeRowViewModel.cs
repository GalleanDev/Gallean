namespace Gallean.Client;

internal sealed record WorldIncomeRowViewModel(WorldResourceType Type, int CapitalIncome, int ResourceNodesIncome, int Income)
{
    public string Name => Type switch
    {
        WorldResourceType.Gold => Tr("Gold"),
        WorldResourceType.LifeMana => Tr("Life mana"),
        WorldResourceType.DeathMana => Tr("Death mana"),
        WorldResourceType.RuneMana => Tr("Rune mana"),
        WorldResourceType.InfernalMana => Tr("Infernal mana"),
        WorldResourceType.GroveMana => Tr("Grove mana")
    };

    public IconMoniker Icon => Type switch
    {
        WorldResourceType.Gold => IconMoniker.Gold,
        WorldResourceType.LifeMana => IconMoniker.ManaLife,
        WorldResourceType.DeathMana => IconMoniker.ManaDeath,
        WorldResourceType.RuneMana => IconMoniker.ManaRune,
        WorldResourceType.InfernalMana => IconMoniker.ManaInfernal,
        WorldResourceType.GroveMana => IconMoniker.ManaGrove
    };

    public string CapitalIncomeText => CapitalIncome.ToString();

    public string ResourceNodesIncomeText => ResourceNodesIncome.ToString();

    public string IncomeText => $"+{Income}";
}
