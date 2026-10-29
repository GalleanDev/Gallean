using System.Collections.Immutable;
using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed record UnitDetailsViewModel(
    AssetSpriteKey.UnitEventPortrait PortraitKey,
    string Name,
    string Description,
    int Level,
    int Hitpoints,
    int MaxHitpoints,
    int Experience,
    int NextLevelExperience,
    bool IsMaximumLevel,
    bool IsPendingUpgrade,
    bool IsUpgradeBlocked,
    bool IsTypeLocked,
    string AttackText,
    UnitAttackSourceViewModel AttackSource,
    StatValueViewModel Damage,
    UnitSecondaryAttackViewModel? SecondaryAttack,
    IReadOnlyList<StatRowViewModel> Stats,
    UnitWardGridViewModel? Immunities,
    UnitWardGridViewModel? Wards,
    StatRowViewModel? Leadership,
    IReadOnlyList<UnitStatusViewModel> Statuses,
    IReadOnlyList<UnitPermanentModifierViewModel> PermanentModifiers)
{
    private static readonly ImmutableArray<Func<UnitWardCategory, UnitWard>> WardTypes =
    [
        category => new UnitAttackSourceWard(UnitAttackSource.Weapon, category),
        category => new UnitAttackSourceWard(UnitAttackSource.Mind, category),
        category => new UnitAttackSourceWard(UnitAttackSource.Life, category),
        category => new UnitAttackSourceWard(UnitAttackSource.Death, category),
        category => new UnitAttackSourceWard(UnitAttackSource.Fire, category),
        category => new UnitAttackSourceWard(UnitAttackSource.Water, category),
        category => new UnitAttackSourceWard(UnitAttackSource.Earth, category),
        category => new UnitAttackSourceWard(UnitAttackSource.Air, category),
        category => new UnitAttackClassWard(UnitAttackType.Poison, category),
        category => new UnitAttackClassWard(UnitAttackType.ReduceArmor, category)
    ];

    public static WorldUnitId PreviewUnitId { get; } = new("preview");

    public string LevelText => Tr("Level {0}", Level);

    public UnitDamageOverTimeViewModel? DamageOverTime => SecondaryAttack?.DamageOverTime;

    public bool Equals(UnitDetailsViewModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return PortraitKey == other.PortraitKey &&
               Name == other.Name &&
               Description == other.Description &&
               Level == other.Level &&
               Hitpoints == other.Hitpoints &&
               MaxHitpoints == other.MaxHitpoints &&
               Experience == other.Experience &&
               NextLevelExperience == other.NextLevelExperience &&
               IsMaximumLevel == other.IsMaximumLevel &&
               IsPendingUpgrade == other.IsPendingUpgrade &&
               IsUpgradeBlocked == other.IsUpgradeBlocked &&
               IsTypeLocked == other.IsTypeLocked &&
               AttackText == other.AttackText &&
               AttackSource == other.AttackSource &&
               Damage == other.Damage &&
               SecondaryAttack == other.SecondaryAttack &&
               Stats.SequenceEqual(other.Stats) &&
               Immunities == other.Immunities &&
               Wards == other.Wards &&
               Leadership == other.Leadership &&
               Statuses.SequenceEqual(other.Statuses) &&
               PermanentModifiers.SequenceEqual(other.PermanentModifiers);
    }

    public static UnitDetailsViewModel CreateFromBattle(
        BattleState state,
        BattleUnitId unitId,
        WorldPlayer? owner,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts,
        IUnitStatsProvider stats)
    {
        BattleUnit unit = state.GetUnit(unitId);
        UnitDefinition definition = definitions.GetUnit(unit.DefinitionId);
        string name = UnitNameResolver.Resolve(unit, definitions, texts);
        UnitStats unitStats = stats.GetBattleUnitStats(state, unitId);
        UnitAttackStats attackStats = stats.GetBattleAttackStats(state, unitId, definition.Attack);
        WorldUnit baseUnit = new(
            PreviewUnitId,
            unit.DefinitionId,
            unit.Hitpoints,
            unit.Experience,
            unit.Level,
            GetScenarioModifiers(unit.Modifiers),
            unit.Name,
            unit.IsTypeLocked);

        UnitStats baseStats = stats.GetWorldUnitStats(baseUnit);
        UnitExperience experience = stats.GetBattleUnitExperience(state, unitId);

        if (state.Outcome is not null)
        {
            experience = experience with { Current = experience.Current + unit.ExperienceGained };
        }

        IReadOnlyList<UnitModifierViewModel> modifiers =
            UnitModifierViewModel.CreateAll(unit, definitions);
        IReadOnlyList<UnitStatusViewModel> statuses =
            UnitStatusViewModel.CreateAll(BattleUnitState.StatusEffect.CreateAll(unit.StatusEffects), modifiers);
        IReadOnlyList<UnitPermanentModifierViewModel> permanentModifiers = UnitPermanentModifierViewModel.CreateAll(
            stats.GetWorldUnitAbilities(baseUnit),
            unit.Modifiers,
            GetLeaderUpgrades(owner, definitions),
            definitions,
            texts);
        bool isUpgradeBlocked = CheckUpgradeBlocked(unit.DefinitionId, unit.IsTypeLocked, experience, owner, stats);

        UnitSecondaryAttackViewModel? secondaryAttack;
        if (definition.SecondaryAttack is not null)
        {
            UnitAttackStats secondaryAttackStats = stats.GetBattleAttackStats(state, unitId, definition.SecondaryAttack);
            secondaryAttack = UnitSecondaryAttackViewModel.Create(definition.SecondaryAttack, secondaryAttackStats.Damage);
        }
        else
        {
            secondaryAttack = null;
        }

        return Build(
            unit.DefinitionId,
            definition,
            name,
            unitStats,
            baseStats,
            attackStats.Damage,
            secondaryAttack,
            unit.Hitpoints,
            unit.Level,
            experience,
            isUpgradeBlocked,
            unit.IsTypeLocked,
            texts,
            unit.Wards,
            stats.GetBattleUnitLeadership(state, unitId),
            statuses,
            permanentModifiers);
    }

    public static UnitDetailsViewModel CreateFromWorld(
        WorldUnit unit,
        WorldPlayer? owner,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts,
        IUnitStatsProvider stats)
    {
        UnitDefinition definition = definitions.GetUnit(unit.DefinitionId);
        string name = UnitNameResolver.Resolve(unit, definitions, texts);
        UnitStats unitStats = stats.GetWorldUnitStats(unit);
        WorldUnit baseUnit = unit with { Modifiers = GetScenarioModifiers(unit.Modifiers) };
        UnitStats baseStats = stats.GetWorldUnitStats(baseUnit);
        IReadOnlyList<UnitModifierViewModel> modifiers = UnitModifierViewModel.CreateAll(unit, definitions);
        UnitExperience experience = stats.GetWorldUnitExperience(unit);
        bool isUpgradeBlocked = CheckUpgradeBlocked(unit.DefinitionId, unit.IsTypeLocked, experience, owner, stats);
        IReadOnlyList<UnitStatusViewModel> statuses = UnitStatusViewModel.CreateAll([], modifiers);
        IReadOnlyList<UnitPermanentModifierViewModel> permanentModifiers = UnitPermanentModifierViewModel.CreateAll(
            stats.GetWorldUnitAbilities(baseUnit),
            unit.Modifiers,
            GetLeaderUpgrades(owner, definitions),
            definitions,
            texts);

        UnitSecondaryAttackViewModel? secondaryAttack;
        if (definition.SecondaryAttack is not null)
        {
            UnitAttackStats secondaryAttackStats = stats.GetWorldAttackStats(unit, definition.SecondaryAttack);
            secondaryAttack = UnitSecondaryAttackViewModel.Create(definition.SecondaryAttack, secondaryAttackStats.Damage);
        }
        else
        {
            secondaryAttack = null;
        }

        return Build(
            unit.DefinitionId,
            definition,
            name,
            unitStats,
            baseStats,
            unitStats.Damage,
            secondaryAttack,
            unit.Hitpoints,
            unit.Level,
            experience,
            isUpgradeBlocked,
            unit.IsTypeLocked,
            texts,
            stats.GetWorldUnitWards(unit),
            stats.GetWorldUnitLeadership(unit),
            statuses,
            permanentModifiers);
    }

    private static ImmutableArray<LeaderUpgradeDefinition> GetLeaderUpgrades(WorldPlayer? owner, IGameDefinitionProvider definitions)
    {
        if (owner is not null)
        {
            return definitions.GetLeaderUpgrades(owner.LordId);
        }
        else
        {
            return [];
        }
    }

    private static ImmutableArray<UnitModifier> GetScenarioModifiers(ImmutableArray<UnitModifier> modifiers)
    {
        return [.. modifiers.Where(modifier => modifier.Source is UnitModifierSource.Scenario)];
    }

    private static UnitDetailsViewModel Build(
        UnitDefinitionId definitionId,
        UnitDefinition definition,
        string name,
        UnitStats unitStats,
        UnitStats baseStats,
        int attackDamage,
        UnitSecondaryAttackViewModel? secondaryAttack,
        int hitpoints,
        int level,
        UnitExperience experience,
        bool isUpgradeBlocked,
        bool isTypeLocked,
        IGameTextProvider texts,
        ImmutableArray<UnitWard> wards,
        int leadership,
        IReadOnlyList<UnitStatusViewModel> statuses,
        IReadOnlyList<UnitPermanentModifierViewModel> permanentModifiers)
    {
        StatValueViewModel damage;
        if (baseStats.Heal > 0)
        {
            damage = new StatValueViewModel(TextSpans.FromText(baseStats.Heal.ToString()), unitStats.Heal - baseStats.Heal);
        }
        else if (baseStats.Damage > 0 && attackDamage > 0)
        {
            damage = new StatValueViewModel(TextSpans.FromText(baseStats.Damage.ToString()), attackDamage - baseStats.Damage);
        }
        else
        {
            damage = new StatValueViewModel(TextSpans.FromText("—"), 0);
        }

        int currentExperience;
        if (experience.IsMaximumLevel)
        {
            currentExperience = experience.NextLevel;
        }
        else
        {
            currentExperience = experience.Current;
        }

        List<StatRowViewModel> rows =
        [
            new(IconMoniker.Target, Tr("Targets"), CreateTargets(definition.Attack.Reach)),
            new(IconMoniker.Armor, Tr("Armor"), new StatValueViewModel(TextSpans.FromText(baseStats.Armor.ToString()), unitStats.Armor - baseStats.Armor)),
            new(IconMoniker.Accuracy, Tr("Accuracy"), new StatValueViewModel(TextSpans.FromText($"{baseStats.Accuracy}%"), unitStats.Accuracy - baseStats.Accuracy)),
            new(IconMoniker.Initiative, Tr("Initiative"), new StatValueViewModel(TextSpans.FromText(baseStats.Initiative.ToString()), unitStats.Initiative - baseStats.Initiative))
        ];

        StatRowViewModel? leadershipRow;
        if (definition.LeaderCategory is not null)
        {
            leadershipRow = new StatRowViewModel(IconMoniker.Leadership, Tr("Leadership"), new StatValueViewModel(TextSpans.FromText(leadership.ToString()), 0));
        }
        else
        {
            leadershipRow = null;
        }

        GlobalText description = texts.GetGlobalText(definition.DescriptionTextId);

        return new UnitDetailsViewModel(
            new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(definitionId)),
            name,
            description.Text,
            level,
            hitpoints,
            unitStats.MaxHitpoints,
            currentExperience,
            experience.NextLevel,
            experience.IsMaximumLevel,
            experience.IsPendingUpgrade && !isUpgradeBlocked,
            isUpgradeBlocked,
            isTypeLocked,
            FormatAttack(definition.Attack, definition.AttacksTwice),
            UnitAttackSourceViewModel.Create(definition.Attack.Source),
            damage,
            secondaryAttack,
            rows,
            CreateWardGrid(wards, UnitWardCategory.Always),
            CreateWardGrid(wards, UnitWardCategory.Once),
            leadershipRow,
            statuses,
            permanentModifiers);
    }

    private static UnitWardGridViewModel? CreateWardGrid(ImmutableArray<UnitWard> wards, UnitWardCategory category)
    {
        List<UnitWardItemViewModel> items = [];
        bool hasActive = false;

        foreach (Func<UnitWardCategory, UnitWard> type in WardTypes)
        {
            UnitWard ward = type(category);
            bool isActive = wards.Contains(ward);

            items.Add(UnitWardItemBuilder.Build(ward, isActive));

            if (isActive)
            {
                hasActive = true;
            }
        }

        if (hasActive)
        {
            return new UnitWardGridViewModel(items);
        }
        else
        {
            return null;
        }
    }

    private static bool CheckUpgradeBlocked(
        UnitDefinitionId definitionId,
        bool isTypeLocked,
        UnitExperience experience,
        WorldPlayer? owner,
        IUnitStatsProvider stats)
    {
        if (owner is not null)
        {
            if (experience.IsMaximumLevel)
            {
                return false;
            }
            else
            {
                UnitUpgrade upgrade = stats.GetUnitUpgrade(definitionId, isTypeLocked, owner.Buildings);

                return upgrade is UnitUpgrade.Blocked;
            }
        }
        else
        {
            return false;
        }
    }

    private static StatValueViewModel CreateTargets(UnitAttackReach reach)
    {
        return reach switch
        {
            UnitAttackReach.All => new StatValueViewModel(TrSpans("{0:b} {1:i}", GridPosition.Count, Tr("(All)")), 0),
            UnitAttackReach.Any => new StatValueViewModel(TrSpans("{0:b} {1:i}", 1, Tr("(Ranged)")), 0),
            UnitAttackReach.Adjacent => new StatValueViewModel(TrSpans("{0:b} {1:i}", 1, Tr("(Melee)")), 0)
        };
    }

    private static string FormatAttack(UnitAttackDefinition attack, bool attacksTwice)
    {
        string text = UnitAttackFormatter.FormatType(attack.Type);

        if (attacksTwice)
        {
            text += " ×2";
        }

        return text;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(PortraitKey);
        hash.Add(Name);
        hash.Add(Description);
        hash.Add(Level);
        hash.Add(Hitpoints);
        hash.Add(MaxHitpoints);
        hash.Add(Experience);
        hash.Add(NextLevelExperience);
        hash.Add(IsMaximumLevel);
        hash.Add(IsPendingUpgrade);
        hash.Add(IsUpgradeBlocked);
        hash.Add(IsTypeLocked);
        hash.Add(AttackText);
        hash.Add(AttackSource);
        hash.Add(Damage);
        hash.Add(SecondaryAttack);
        hash.AddSequence(Stats);
        hash.Add(Immunities);
        hash.Add(Wards);
        hash.Add(Leadership);
        hash.AddSequence(Statuses);
        hash.AddSequence(PermanentModifiers);

        return hash.ToHashCode();
    }
}
