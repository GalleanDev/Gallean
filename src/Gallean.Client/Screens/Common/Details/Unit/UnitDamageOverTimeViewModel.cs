namespace Gallean.Client;

internal sealed record UnitDamageOverTimeViewModel(UnitStatusViewModel Status, int Damage)
{
    public string Text => Tr("{0} per turn", Damage);
}
