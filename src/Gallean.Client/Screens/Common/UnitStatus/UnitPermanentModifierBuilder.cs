using System.Collections.Immutable;
using Gallean.Engine;

namespace Gallean.Client;

internal static class UnitPermanentModifierBuilder
{
    private sealed record Stat(
        Func<UnitModifierDefinition, UnitStatModifier> Get,
        Func<UnitModifierDefinition, UnitStatModifier, UnitModifierDefinition> Set)
    {
        public static ImmutableArray<Stat> All { get; } =
        [
            new(modifier => modifier.Hitpoints, (modifier, value) => modifier with { Hitpoints = value }),
            new(modifier => modifier.Armor, (modifier, value) => modifier with { Armor = value }),
            new(modifier => modifier.Damage, (modifier, value) => modifier with { Damage = value }),
            new(modifier => modifier.Accuracy, (modifier, value) => modifier with { Accuracy = value }),
            new(modifier => modifier.Initiative, (modifier, value) => modifier with { Initiative = value }),
            new(modifier => modifier.Leadership, (modifier, value) => modifier with { Leadership = value }),
            new(modifier => modifier.MaxMovement, (modifier, value) => modifier with { MaxMovement = value }),
            new(modifier => modifier.VisionRadius, (modifier, value) => modifier with { VisionRadius = value })
        ];
    }

    public static IReadOnlyList<UnitPermanentModifierViewModel> Build(
        LeaderAbility baseAbilities,
        UnitMoveAbility baseMoveAbility,
        ImmutableArray<UnitModifier> modifiers,
        ImmutableArray<LeaderUpgradeDefinition> leaderUpgrades,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts)
    {
        List<(LeaderUpgradeDefinition Upgrade, UnitModifier Modifier)> upgradeModifiers = [];
        List<UnitModifier> otherModifiers = [];

        foreach (UnitModifier modifier in modifiers)
        {
            if (modifier.RemainingTurns is null && modifier.Source is not UnitModifierSource.Scenario)
            {
                LeaderUpgradeDefinition? upgrade = FindLeaderUpgrade(modifier, leaderUpgrades);

                if (upgrade is not null)
                {
                    upgradeModifiers.Add((upgrade, modifier));
                }
                else
                {
                    otherModifiers.Add(modifier);
                }
            }
        }

        List<UnitPermanentModifierViewModel> result = [];

        result.AddRange(BuildAbilities(baseAbilities, texts));
        result.AddRange(BuildMoveAbility(baseMoveAbility, texts));
        result.AddRange(BuildLeaderUpgrades(upgradeModifiers, definitions, texts));
        result.AddRange(BuildEffects(otherModifiers, definitions, texts));
        result.AddRange(BuildStats(otherModifiers, definitions, texts));

        return result;
    }

    private static IEnumerable<UnitPermanentModifierViewModel> BuildAbilities(LeaderAbility abilities, IGameTextProvider texts)
    {
        foreach (LeaderAbility ability in Enum.GetValues<LeaderAbility>())
        {
            if (ability != LeaderAbility.None && abilities.HasFlag(ability))
            {
                yield return new UnitPermanentModifierViewModel(
                    UnitPermanentModifierType.Skill,
                    LeaderAbilityIconResolver.Resolve(ability),
                    LeaderAbilityFormatter.FormatName(ability, texts),
                    LeaderAbilityFormatter.FormatDescription(ability, texts),
                    Tr("Innate"),
                    1,
                    []);
            }
        }
    }

    private static IEnumerable<UnitPermanentModifierViewModel> BuildMoveAbility(UnitMoveAbility ability, IGameTextProvider texts)
    {
        string? name = UnitMoveAbilityFormatter.Format(ability);
        string? description = UnitMoveAbilityFormatter.FormatDescription(ability, texts);

        if (name is not null && description is not null)
        {
            yield return new UnitPermanentModifierViewModel(
                UnitPermanentModifierType.Skill,
                UnitMoveAbilityIconResolver.Resolve(ability),
                name,
                description,
                Tr("Innate"),
                1,
                []);
        }
    }

    private static IEnumerable<UnitPermanentModifierViewModel> BuildLeaderUpgrades(
        IReadOnlyList<(LeaderUpgradeDefinition Upgrade, UnitModifier Modifier)> modifiers,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts)
    {
        ILookup<(string Name, string Description), UnitModifierDefinition> groups = modifiers.ToLookup(
            entry => FormatLeaderUpgrade(entry.Upgrade, texts),
            entry => definitions.GetUnitModifier(entry.Modifier.DefinitionId));

        foreach (IGrouping<(string Name, string Description), UnitModifierDefinition> group in groups)
        {
            UnitModifierBuilder builder = new UnitModifierBuilder().AddRange(group);

            if (builder.TryBuild(out UnitModifierDefinition? definition))
            {
                yield return new UnitPermanentModifierViewModel(
                    ResolveType(definition),
                    UnitModifierIconResolver.Resolve(definition),
                    group.Key.Name,
                    group.Key.Description,
                    FormatEffects(definition, texts),
                    group.Count(),
                    []);
            }
        }
    }

    private static IEnumerable<UnitPermanentModifierViewModel> BuildEffects(
        IReadOnlyList<UnitModifier> modifiers,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts)
    {
        ILookup<(string Effects, UnitModifierSource Source), UnitModifierDefinition> groups = modifiers.ToLookup(
            modifier => (FormatEffects(WithoutStats(definitions.GetUnitModifier(modifier.DefinitionId)), texts), modifier.Source),
            modifier => WithoutStats(definitions.GetUnitModifier(modifier.DefinitionId)));

        foreach (IGrouping<(string Effects, UnitModifierSource Source), UnitModifierDefinition> group in groups)
        {
            UnitModifierBuilder builder = new UnitModifierBuilder().AddRange(group);

            if (group.Key.Effects.Length > 0 && builder.TryBuild(out UnitModifierDefinition? definition))
            {
                yield return new UnitPermanentModifierViewModel(
                    ResolveType(definition),
                    UnitModifierIconResolver.Resolve(definition),
                    FormatEffects(definition, texts),
                    FormatSource(group.Key.Source),
                    null,
                    group.Count(),
                    []);
            }
        }
    }

    private static IEnumerable<UnitPermanentModifierViewModel> BuildStats(
        IReadOnlyList<UnitModifier> modifiers,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts)
    {
        foreach (Stat stat in Stat.All)
        {
            UnitModifierBuilder builder = new();

            foreach (UnitModifier modifier in modifiers)
            {
                UnitModifierDefinition definition = definitions.GetUnitModifier(modifier.DefinitionId);

                builder.Add(WithOnlyStat(definition, stat));
            }

            if (builder.TryBuild(out UnitModifierDefinition? total) && stat.Get(total) != default)
            {
                yield return BuildStat(total, BuildSources(stat, modifiers, definitions), texts);
            }
        }
    }

    private static UnitPermanentModifierViewModel BuildStat(
        UnitModifierDefinition definition,
        IReadOnlyList<UnitPermanentModifierSourceViewModel> sources,
        IGameTextProvider texts)
    {
        UnitPermanentModifierType type = ResolveType(definition);
        IconMoniker icon = UnitModifierIconResolver.Resolve(definition);
        string name = FormatEffects(definition, texts);

        if (sources.Count > 1)
        {
            return new UnitPermanentModifierViewModel(type, icon, name, null, null, 1, sources);
        }
        else
        {
            return new UnitPermanentModifierViewModel(type, icon, name, sources[0].Name, null, 1, []);
        }
    }

    private static IReadOnlyList<UnitPermanentModifierSourceViewModel> BuildSources(
        Stat stat,
        IReadOnlyList<UnitModifier> modifiers,
        IGameDefinitionProvider definitions)
    {
        List<UnitPermanentModifierSourceViewModel> result = [];

        foreach (UnitModifier modifier in modifiers)
        {
            UnitStatModifier value = stat.Get(definitions.GetUnitModifier(modifier.DefinitionId));

            if (value != default)
            {
                result.Add(new UnitPermanentModifierSourceViewModel(
                    FormatSource(modifier.Source),
                    string.Join(", ", UnitModifierFormatter.FormatValues(value))));
            }
        }

        return result;
    }

    private static LeaderUpgradeDefinition? FindLeaderUpgrade(UnitModifier modifier, ImmutableArray<LeaderUpgradeDefinition> leaderUpgrades)
    {
        if (modifier.Source is UnitModifierSource.Unit)
        {
            foreach (LeaderUpgradeDefinition upgrade in leaderUpgrades)
            {
                if (upgrade.ModifierId == modifier.DefinitionId)
                {
                    return upgrade;
                }
            }
        }

        return null;
    }

    private static UnitModifierDefinition WithOnlyStat(UnitModifierDefinition modifier, Stat stat)
    {
        UnitModifierDefinition empty = WithoutStats(modifier) with
        {
            MoveAbility = UnitMoveAbility.None,
            RegenerationPercent = 0,
            DrainPercent = 0,
            FastRetreat = false,
            PurchaseDiscountPercent = 0,
            LeaderAbilities = LeaderAbility.None,
            Wards = []
        };

        return stat.Set(empty, stat.Get(modifier));
    }

    private static UnitModifierDefinition WithoutStats(UnitModifierDefinition modifier)
    {
        UnitModifierDefinition result = modifier;

        foreach (Stat stat in Stat.All)
        {
            result = stat.Set(result, default);
        }

        return result;
    }

    private static UnitPermanentModifierType ResolveType(UnitModifierDefinition modifier)
    {
        if (modifier.LeaderAbilities != LeaderAbility.None)
        {
            return UnitPermanentModifierType.Skill;
        }
        else if (modifier.Wards.Length > 0)
        {
            return UnitPermanentModifierType.Protection;
        }
        else if (modifier.Leadership.Flat > 0 || modifier.Leadership.Percent > 0)
        {
            return UnitPermanentModifierType.Leadership;
        }
        else
        {
            return UnitPermanentModifierType.Common;
        }
    }

    private static (string Name, string Description) FormatLeaderUpgrade(LeaderUpgradeDefinition upgrade, IGameTextProvider texts)
    {
        GlobalText name = texts.GetGlobalText(upgrade.NameTextId);
        GlobalText description = texts.GetGlobalText(upgrade.DescriptionTextId);

        return (name.Text, description.Text);
    }

    private static string FormatEffects(UnitModifierDefinition modifier, IGameTextProvider texts)
    {
        return string.Join(", ", UnitModifierFormatter.FormatEffects(modifier, texts));
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
