using Gallean.Engine;

namespace Gallean.Client;

internal sealed record ItemAttackViewModel(string? Amount, UnitAttackViewModel Attack)
{
    public static ItemAttackViewModel Create(UnitAttackDefinition attack)
    {
        string? amount;

        if (attack.Heal > 0)
        {
            amount = attack.Heal.ToString();
        }
        else if (attack.Damage > 0)
        {
            amount = attack.Damage.ToString();
        }
        else
        {
            amount = null;
        }

        return new ItemAttackViewModel(amount, UnitAttackViewModel.Create(attack, attacksTwice: false));
    }
}
