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
            LeaderAbility.WeaponMaster => Tr("Weapon master"),
            LeaderAbility.WandScrollUse => Tr("Wand and scroll use"),
            LeaderAbility.WeaponArmorUse => Tr("Weapon and armor use"),
            LeaderAbility.BannerUse => Tr("Banner use"),
            LeaderAbility.JewelryUse => Tr("Jewelry use"),
            LeaderAbility.Rod => Tr("Rod planting"),
            LeaderAbility.OrbUse => Tr("Orb use"),
            LeaderAbility.TalismanUse => Tr("Talisman use"),
            LeaderAbility.TravelItemUse => Tr("Travel item use"),
            LeaderAbility.CriticalHit => Tr("Critical hit"),
            _ => throw new ArgumentOutOfRangeException(nameof(ability), ability, null)
        };
    }
}
