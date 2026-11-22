using Gallean.Engine;

namespace Gallean.Client;

internal sealed record UnitAttackViewModel(string Text, UnitAttackSourceViewModel Source, bool AttacksTwice)
{
    public static UnitAttackViewModel Create(UnitAttackDefinition attack, bool attacksTwice)
    {
        return new UnitAttackViewModel(UnitAttackFormatter.FormatType(attack.Type), UnitAttackSourceViewModel.Create(attack.Source), attacksTwice);
    }
}
