using Gallean.Engine;

namespace Gallean.Client;

internal static class LeaderAbilityFormatter
{
    public static IReadOnlyList<string> Format(LeaderAbility abilities, IGameTextProvider texts)
    {
        List<string> result = [];

        foreach (LeaderAbility ability in Enum.GetValues<LeaderAbility>())
        {
            if (ability != LeaderAbility.None && abilities.HasFlag(ability))
            {
                result.Add(FormatName(ability, texts));
            }
        }

        return result;
    }

    public static string FormatName(LeaderAbility ability, IGameTextProvider texts)
    {
        return ability switch
        {
            LeaderAbility.Zealot => string.Empty,
            LeaderAbility.Incorruptible => FormatGlobalText(texts, "X000TGA018"),
            LeaderAbility.WeaponMaster => FormatGlobalText(texts, "X000TGA012"),
            LeaderAbility.WandScrollUse => FormatGlobalText(texts, "X000TG6029"),
            LeaderAbility.WeaponArmorUse => FormatGlobalText(texts, "X000TGA001"),
            LeaderAbility.BannerUse => FormatGlobalText(texts, "X000TGA003"),
            LeaderAbility.JewelryUse => FormatGlobalText(texts, "X000TGA002"),
            LeaderAbility.Rod => FormatGlobalText(texts, "X000TGB004"),
            LeaderAbility.OrbUse => FormatGlobalText(texts, "X000TGA005"),
            LeaderAbility.TalismanUse => FormatGlobalText(texts, "X000TGA010"),
            LeaderAbility.TravelItemUse => FormatGlobalText(texts, "X000TGA007"),
            LeaderAbility.CriticalHit => FormatGlobalText(texts, "X160TGC253"),
            _ => throw new ArgumentOutOfRangeException(nameof(ability), ability, null)
        };
    }

    private static string FormatGlobalText(IGameTextProvider texts, string id)
    {
        GlobalText text = texts.GetGlobalText(new GlobalTextId(id));

        return text.Text;
    }
}
