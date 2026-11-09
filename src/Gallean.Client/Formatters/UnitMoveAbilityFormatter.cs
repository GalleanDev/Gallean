using Gallean.Engine;

namespace Gallean.Client;

internal static class UnitMoveAbilityFormatter
{
    public static string? Format(UnitMoveAbility ability)
    {
        return ability switch
        {
            UnitMoveAbility.None => null,
            UnitMoveAbility.Forest => Tr("Forest movement"),
            UnitMoveAbility.Water => Tr("Water movement"),
            UnitMoveAbility.Flying => Tr("Flight")
        };
    }

    public static string? FormatDescription(UnitMoveAbility ability, IGameTextProvider texts)
    {
        return ability switch
        {
            UnitMoveAbility.None => null,
            UnitMoveAbility.Forest => FormatGlobalText(texts, "X000TGA120"),
            UnitMoveAbility.Water => Tr("The leader will suffer no move penalty on water"),
            UnitMoveAbility.Flying => Tr("Allows the leader to Fly")
        };
    }

    private static string FormatGlobalText(IGameTextProvider texts, string id)
    {
        GlobalText text = texts.GetGlobalText(new GlobalTextId(id));

        return text.Text;
    }
}
