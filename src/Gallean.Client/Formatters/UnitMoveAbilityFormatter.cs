using Gallean.Engine;

namespace Gallean.Client;

internal static class UnitMoveAbilityFormatter
{
    public static string? Format(UnitMoveAbility ability)
    {
        if (ability.HasFlag(UnitMoveAbility.Flying))
        {
            return Tr("Flight");
        }
        else if (ability.HasFlag(UnitMoveAbility.Forest))
        {
            return Tr("Forest movement");
        }
        else if (ability.HasFlag(UnitMoveAbility.Water))
        {
            return Tr("Water movement");
        }
        else
        {
            return null;
        }
    }

    public static string? FormatDescription(UnitMoveAbility ability, IGameTextProvider texts)
    {
        if (ability.HasFlag(UnitMoveAbility.Flying))
        {
            return Tr("Allows the leader to Fly");
        }
        else if (ability.HasFlag(UnitMoveAbility.Forest))
        {
            return FormatGlobalText(texts, "X000TGA120");
        }
        else if (ability.HasFlag(UnitMoveAbility.Water))
        {
            return Tr("The leader will suffer no move penalty on water");
        }
        else
        {
            return null;
        }
    }

    private static string FormatGlobalText(IGameTextProvider texts, string id)
    {
        GlobalText text = texts.GetGlobalText(new GlobalTextId(id));

        return text.Text;
    }
}
