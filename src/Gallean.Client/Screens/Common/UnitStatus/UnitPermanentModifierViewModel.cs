using System.Collections.Immutable;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed record UnitPermanentModifierViewModel(IconMoniker Icon, string Name, string Description, string? Effects, int Count)
{
    public string CountText => $"×{Count}";

    public static IReadOnlyList<UnitPermanentModifierViewModel> CreateAll(
        LeaderAbility baseAbilities,
        ImmutableArray<UnitModifier> modifiers,
        ImmutableArray<LeaderUpgradeDefinition> leaderUpgrades,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts)
    {
        return
        [
            .. Enum.GetValues<LeaderAbility>()
                .Where(ability => ability != LeaderAbility.None && baseAbilities.HasFlag(ability))
                .Select(Create),
            .. modifiers
                .Where(modifier => modifier.RemainingTurns is null && modifier.Source is not UnitModifierSource.Scenario)
                .Select(modifier => Create(modifier, leaderUpgrades, definitions, texts))
                .GroupBy(modifier => modifier)
                .Select(group => group.Key with { Count = group.Count() })
        ];
    }

    private static UnitPermanentModifierViewModel Create(LeaderAbility ability)
    {
        return new UnitPermanentModifierViewModel(LeaderAbilityIconResolver.Resolve(ability), LeaderAbilityFormatter.FormatName(ability), Tr("Innate"), null, 1);
    }

    private static UnitPermanentModifierViewModel Create(
        UnitModifier modifier,
        ImmutableArray<LeaderUpgradeDefinition> leaderUpgrades,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts)
    {
        UnitModifierDefinition modifierDefinition = definitions.GetUnitModifier(modifier.DefinitionId);
        IconMoniker icon = UnitModifierIconResolver.Resolve(modifierDefinition);
        string effects = string.Join(", ", UnitModifierFormatter.FormatEffects(modifierDefinition));
        LeaderUpgradeDefinition? upgradeDefinition = leaderUpgrades.FirstOrDefault(upgrade => upgrade.ModifierId == modifier.DefinitionId);

        if (modifier.Source is UnitModifierSource.Unit && upgradeDefinition is not null)
        {
            GlobalText name = texts.GetGlobalText(upgradeDefinition.NameTextId);
            GlobalText description = texts.GetGlobalText(upgradeDefinition.DescriptionTextId);

            return new UnitPermanentModifierViewModel(icon, name.Text, description.Text, effects, 1);
        }
        else
        {
            return new UnitPermanentModifierViewModel(icon, effects, FormatSource(modifier.Source), null, 1);
        }
    }

    private static string FormatSource(UnitModifierSource source)
    {
        return source switch
        {
            UnitModifierSource.Unit => Tr("Permanent"),
            UnitModifierSource.Equipment => Tr("From equipment"),
            UnitModifierSource.Scenario => Tr("Permanent"),
            UnitModifierSource.Fortification => Tr("From city")
        };
    }
}
