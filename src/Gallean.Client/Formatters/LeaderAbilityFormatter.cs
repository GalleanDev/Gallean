using Gallean.Engine;

namespace Gallean.Client;

internal static class LeaderAbilityFormatter
{
    public static IReadOnlyList<string> Format(LeaderAbility abilities)
    {
        List<string> result = [];

        foreach (LeaderAbility ability in Enum.GetValues<LeaderAbility>())
        {
            if (ability != LeaderAbility.None && abilities.HasFlag(ability))
            {
                result.Add(FormatName(ability));
            }
        }

        return result;
    }

    public static string FormatName(LeaderAbility ability)
    {
        return ability switch
        {
            LeaderAbility.Zealot => Tr("Zealot"),
            LeaderAbility.Incorruptible => Tr("Incorruptible"),
            LeaderAbility.WeaponMaster => Tr("Weapon Master"),
            LeaderAbility.WandScrollUse => Tr("Read Parchments"),
            LeaderAbility.WeaponArmorUse => Tr("Artifact Lore"),
            LeaderAbility.BannerUse => Tr("Banner Bearer"),
            LeaderAbility.JewelryUse => Tr("Arcane Knowledge"),
            LeaderAbility.Rod => Tr("Plants Rods"),
            LeaderAbility.OrbUse => Tr("Arcane Lore"),
            LeaderAbility.TalismanUse => Tr("Arcane Power"),
            LeaderAbility.TravelItemUse => Tr("Travel Lore"),
            LeaderAbility.CriticalHit => Tr("Critical Hit"),
            _ => throw new ArgumentOutOfRangeException(nameof(ability), ability, null)
        };
    }
}
