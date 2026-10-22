using Gallean.Common;

namespace Gallean.Client;

internal static class WorldResourceFormatter
{
    public static string Format(WorldResourceSet resources)
    {
        if (resources == WorldResourceSet.Empty)
        {
            return Tr("0 gold");
        }
        else
        {
            JoinStringBuilder sb = new(", ");

            Append(sb, resources.Gold, Tr("gold"));
            Append(sb, resources.LifeMana, Tr("life mana"));
            Append(sb, resources.DeathMana, Tr("death mana"));
            Append(sb, resources.RuneMana, Tr("rune mana"));
            Append(sb, resources.InfernalMana, Tr("infernal mana"));
            Append(sb, resources.GroveMana, Tr("grove mana"));

            return sb.ToString();
        }

        static void Append(JoinStringBuilder parts, int amount, string label)
        {
            if (amount > 0)
            {
                parts.Add($"{amount} {label}");
            }
        }
    }
}
