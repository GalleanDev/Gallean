using Gallean.Engine;

namespace Gallean.Client;

internal static class LeaderAbilityIconResolver
{
    public static IconMoniker Resolve(LeaderAbility ability)
    {
        return ability switch
        {
            LeaderAbility.Zealot => IconMoniker.AbilityZealot,
            LeaderAbility.Incorruptible => IconMoniker.AbilityIncorruptible,
            LeaderAbility.WeaponMaster => IconMoniker.AbilityWeaponMaster,
            LeaderAbility.WandScrollUse => IconMoniker.AbilityWandScrollUse,
            LeaderAbility.WeaponArmorUse => IconMoniker.AbilityWeaponArmorUse,
            LeaderAbility.BannerUse => IconMoniker.AbilityBannerUse,
            LeaderAbility.JewelryUse => IconMoniker.AbilityJewelryUse,
            LeaderAbility.Rod => IconMoniker.AbilityRod,
            LeaderAbility.OrbUse => IconMoniker.AbilityOrbUse,
            LeaderAbility.TalismanUse => IconMoniker.AbilityTalismanUse,
            LeaderAbility.TravelItemUse => IconMoniker.AbilityTravelItemUse,
            LeaderAbility.CriticalHit => IconMoniker.AbilityCriticalHit,
            _ => throw new ArgumentOutOfRangeException(nameof(ability), ability, null)
        };
    }
}
