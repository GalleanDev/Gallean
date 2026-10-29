using Gallean.Engine;

namespace Gallean.Client;

internal sealed record UnitAttackViewModel(string Text, UnitAttackSourceViewModel Source)
{
    public static UnitAttackViewModel Create(UnitAttackDefinition attack, bool attacksTwice)
    {
        string text = UnitAttackFormatter.FormatType(attack.Type);

        if (attacksTwice)
        {
            text += " ×2";
        }

        return new UnitAttackViewModel(text, UnitAttackSourceViewModel.Create(attack.Source));
    }
}
