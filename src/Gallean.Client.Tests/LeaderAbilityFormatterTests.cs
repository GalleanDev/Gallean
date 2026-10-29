using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client.Tests;

public sealed class LeaderAbilityFormatterTests
{
    [Test]
    public void OriginalNamesTest()
    {
        (_, IGameTextProvider texts, _) = new GameLoader(new RandomProvider(0)).LoadCampaigns();
        LeaderAbility abilities =
            LeaderAbility.Zealot |
            LeaderAbility.Incorruptible |
            LeaderAbility.WeaponMaster |
            LeaderAbility.WandScrollUse |
            LeaderAbility.WeaponArmorUse |
            LeaderAbility.BannerUse |
            LeaderAbility.JewelryUse |
            LeaderAbility.Rod |
            LeaderAbility.OrbUse |
            LeaderAbility.TalismanUse |
            LeaderAbility.TravelItemUse |
            LeaderAbility.CriticalHit;

        Assert.That(LeaderAbilityFormatter.Format(abilities, texts), Is.EqualTo([
            string.Empty,
            "Incorruptible",
            "Weapon Master",
            "Read Parchments",
            "Artifact Lore",
            "Banner Bearer",
            "Arcane Knowledge",
            "Plants Rods",
            "Arcane Lore",
            "Arcane Power",
            "Travel Lore",
            "Critical Hit"
        ]));
    }
}
