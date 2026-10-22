using Gallean.Engine;

namespace Gallean.Client;

internal sealed record LeaderAbilityViewModel(IconMoniker Icon, string Name)
{
    public static IReadOnlyList<LeaderAbilityViewModel> CreateList(LeaderAbility abilities)
    {
        List<LeaderAbilityViewModel> result = [];

        foreach (LeaderAbility ability in Enum.GetValues<LeaderAbility>())
        {
            if (ability != LeaderAbility.None && abilities.HasFlag(ability))
            {
                result.Add(new LeaderAbilityViewModel(LeaderAbilityIconResolver.Resolve(ability), LeaderAbilityFormatter.FormatName(ability)));
            }
        }

        return result;
    }
}
