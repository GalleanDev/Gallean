using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Gallean.Engine;

public sealed class GameDefinitionProvider : IGameDefinitionProvider
{
    private readonly FrozenDictionary<WorldAiAttitudeType, WorldAiAttitudeDefinition> aiAttitudes;
    private readonly FrozenDictionary<UnitAttackDefinitionId, UnitAttackDefinition> attacks;
    private readonly FrozenDictionary<WorldCityDefinitionId, WorldCityDefinition> cities;
    private readonly FrozenDictionary<UnitDefinitionId, UnitDynamicUpgradeDefinition> dynamicUpgrades;
    private readonly FrozenDictionary<ItemDefinitionId, ItemDefinition> items;
    private readonly FrozenDictionary<WorldLandmarkDefinitionId, WorldLandmarkDefinition> landmarks;
    private readonly FrozenDictionary<UnitDefinitionId, LeaderAbility> leaderAbilities;
    private readonly FrozenDictionary<LordDefinitionId, ImmutableArray<LeaderUpgradeDefinition>> leaderUpgrades;
    private readonly FrozenDictionary<LordDefinitionId, LordDefinition> lords;
    private readonly FrozenDictionary<UnitDefinitionId, UnitMoveAbility> moveAbilities;
    private readonly FrozenDictionary<WorldPlayerBuildingDefinitionId, WorldPlayerBuildingDefinition> playerBuildings;
    private readonly FrozenDictionary<WorldRace, WorldRaceDefinition> races;
    private readonly WorldSpellResearchCosts spellResearchCosts;
    private readonly FrozenDictionary<WorldSpellDefinitionId, WorldSpellDefinition> spells;
    private readonly FrozenDictionary<WorldThiefActionType, WorldThiefActionDefinition> thiefActions;
    private readonly FrozenDictionary<UnitModifierDefinitionId, UnitModifierDefinition> unitModifiers;
    private readonly FrozenDictionary<UnitDefinitionId, UnitDefinition> units;
    private readonly FrozenDictionary<UnitDefinitionId, ImmutableArray<UnitUpgradeDefinition>> unitUpgrades;
    private readonly FrozenDictionary<UnitDefinitionId, ImmutableArray<UnitWard>> unitWards;

    public GameDefinitionProvider(GlobalDatabase globalDb)
    {
        ImmutableArray<WorldCityDefinition> cityDefinitions = [.. LoadCities(globalDb)];

        aiAttitudes = LoadAiAttitudes(globalDb).ToFrozenDictionary(a => a.Type);
        attacks = LoadAttacks(globalDb).ToFrozenDictionary(attack => attack.Id);
        cities = cityDefinitions.ToFrozenDictionary(c => c.Id);
        dynamicUpgrades = LoadDynamicUpgrades(globalDb).ToFrozenDictionary(upgrade => upgrade.UnitDefinitionId);
        items = LoadItems(globalDb).ToFrozenDictionary(item => item.Id);
        landmarks = LoadLandmarks(globalDb).ToFrozenDictionary(landmark => landmark.Id);
        leaderAbilities = LoadLeaderAbilities(globalDb);
        leaderUpgrades = LoadLeaderUpgrades(globalDb);
        lords = LoadLords(globalDb).ToFrozenDictionary(lord => lord.Id);
        moveAbilities = LoadMoveAbilities(globalDb);
        playerBuildings = LoadPlayerBuildings(globalDb).ToFrozenDictionary(building => building.Id);
        races = LoadRaces(globalDb, cityDefinitions).ToFrozenDictionary(r => r.Race);
        spellResearchCosts = new WorldSpellResearchCosts(LoadSpellResearchCosts(globalDb));
        spells = LoadSpells(globalDb).ToFrozenDictionary(spell => spell.Id);
        thiefActions = LoadThiefActions(globalDb).ToFrozenDictionary(action => action.Type);
        unitModifiers = LoadUnitModifiers(globalDb).ToFrozenDictionary(modifier => modifier.Id);
        unitUpgrades = LoadUnitUpgrades(globalDb);
        unitWards = LoadUnitWards(globalDb);
        units = LoadUnits(globalDb).ToFrozenDictionary(unit => unit.Id);
    }

    public UnitDefinition GetUnit(UnitDefinitionId id)
    {
        if (units.TryGetValue(id, out UnitDefinition? definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Unit definition '{id}' was not found.");
    }

    public UnitDefinition? TryGetUnit(UnitDefinitionId id)
    {
        return units.GetValueOrDefault(id);
    }

    public UnitAttackDefinition GetAttack(UnitAttackDefinitionId id)
    {
        if (attacks.TryGetValue(id, out UnitAttackDefinition? definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Attack definition '{id}' was not found.");
    }

    public ImmutableArray<UnitUpgradeDefinition> GetUnitUpgrades(UnitDefinitionId id)
    {
        return unitUpgrades.TryGetValue(id, out ImmutableArray<UnitUpgradeDefinition> upgrades) ? upgrades : [];
    }

    public UnitDynamicUpgradeDefinition GetUnitDynamicUpgrade(UnitDefinitionId id)
    {
        if (dynamicUpgrades.TryGetValue(id, out UnitDynamicUpgradeDefinition? definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Dynamic upgrade definition for unit '{id}' was not found.");
    }

    public LordDefinition GetLord(LordDefinitionId id)
    {
        if (lords.TryGetValue(id, out LordDefinition? definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Lord definition '{id}' was not found.");
    }

    public ImmutableArray<LeaderUpgradeDefinition> GetLeaderUpgrades(LordDefinitionId id)
    {
        return leaderUpgrades.TryGetValue(id, out ImmutableArray<LeaderUpgradeDefinition> upgrades) ? upgrades : [];
    }

    public UnitModifierDefinition GetUnitModifier(UnitModifierDefinitionId id)
    {
        if (unitModifiers.TryGetValue(id, out UnitModifierDefinition? definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Unit modifier definition '{id}' was not found.");
    }

    public ItemDefinition GetItem(ItemDefinitionId id)
    {
        if (items.TryGetValue(id, out ItemDefinition? definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Item definition '{id}' was not found.");
    }

    public WorldLandmarkDefinition GetLandmark(WorldLandmarkDefinitionId id)
    {
        if (landmarks.TryGetValue(id, out WorldLandmarkDefinition? definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Landmark definition '{id}' was not found.");
    }

    public WorldPlayerBuildingDefinition GetPlayerBuilding(WorldPlayerBuildingDefinitionId id)
    {
        if (playerBuildings.TryGetValue(id, out WorldPlayerBuildingDefinition? definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Building definition '{id}' was not found.");
    }

    public WorldCityDefinition GetCity(WorldCityDefinitionId id)
    {
        if (cities.TryGetValue(id, out WorldCityDefinition? definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"City definition '{id}' was not found.");
    }

    public WorldAiAttitudeDefinition GetAiAttitude(WorldAiAttitudeType type)
    {
        if (aiAttitudes.TryGetValue(type, out WorldAiAttitudeDefinition? definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"AI attitude definition '{type}' was not found.");
    }

    private static IEnumerable<WorldAiAttitudeDefinition> LoadAiAttitudes(GlobalDatabase globalDb)
    {
        foreach (Db.Ai attitude in globalDb.Ais)
        {
            yield return new WorldAiAttitudeDefinition(
                MapAiAttitudeType(attitude.Category),
                attitude.InterestStacksNeutral,
                attitude.InterestStacksRival,
                attitude.InterestStacksPlayer,
                attitude.InterestCitiesNeutral,
                attitude.InterestCitiesRival,
                attitude.InterestCitiesPlayer,
                attitude.InterestBags,
                attitude.InterestRuins,
                attitude.DifficultyEasyResourcePercent,
                attitude.DifficultyAverageResourcePercent,
                attitude.DifficultyHardResourcePercent,
                attitude.DifficultyVeryHardResourcePercent);
        }
    }

    private static WorldAiAttitudeType MapAiAttitudeType(int category)
    {
        return (Db.AiAttitudeType)category switch
        {
            Db.AiAttitudeType.Small => WorldAiAttitudeType.Small,
            Db.AiAttitudeType.Medium => WorldAiAttitudeType.Medium,
            Db.AiAttitudeType.Large => WorldAiAttitudeType.Large,
            Db.AiAttitudeType.Humongous => WorldAiAttitudeType.Humongous,
            Db.AiAttitudeType.SmallNoResource => WorldAiAttitudeType.SmallNoExpansion,
            Db.AiAttitudeType.MediumNoResource => WorldAiAttitudeType.MediumNoExpansion,
            Db.AiAttitudeType.MediumNoResourceRuin => WorldAiAttitudeType.MediumNoExpansionNoRuins
        };
    }

    public WorldRaceDefinition GetRace(WorldRace race)
    {
        if (races.TryGetValue(race, out WorldRaceDefinition? definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Race definition '{race}' was not found.");
    }

    public WorldSpellDefinition GetSpell(WorldSpellDefinitionId id)
    {
        if (spells.TryGetValue(id, out WorldSpellDefinition? definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Spell definition '{id}' was not found.");
    }

    public WorldThiefActionDefinition GetThiefAction(WorldThiefActionType type)
    {
        if (thiefActions.TryGetValue(type, out WorldThiefActionDefinition? definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Thief action definition '{type}' was not found.");
    }

    public IReadOnlyDictionary<WorldSpellDefinitionId, WorldResourceSet> GetSpellResearchCosts(WorldRace race)
    {
        return spellResearchCosts.GetSpellResearchCosts(race);
    }

    private IEnumerable<UnitDefinition> LoadUnits(GlobalDatabase globalDb)
    {
        foreach (Db.Unit unit in globalDb.Units.Values)
        {
            yield return CreateUnit(globalDb, new UnitDefinitionId(unit.UnitId), unit);
        }
    }

    private UnitDefinition CreateUnit(GlobalDatabase globalDb, UnitDefinitionId id, Db.Unit unit)
    {
        UnitAttackDefinition attack = ConvertAttack(globalDb, GetAttack(globalDb, unit.AttackId, id));

        UnitAttackDefinition? secondaryAttack;
        if (unit.Attack2Id is not null)
        {
            secondaryAttack = ConvertAttack(globalDb, GetAttack(globalDb, unit.Attack2Id.Value, id));
        }
        else
        {
            secondaryAttack = null;
        }

        ImmutableArray<UnitWard> wards;
        if (unitWards.TryGetValue(id, out ImmutableArray<UnitWard> values))
        {
            wards = values;
        }
        else
        {
            wards = [];
        }

        if (!globalDb.Races.TryGetValue(unit.RaceId, out Db.Race? race))
        {
            throw new InvalidDataException($"Race '{unit.RaceId}' for unit '{id}' was not found.");
        }

        return new UnitDefinition(
            id,
            new GlobalTextId(unit.NameText),
            new GlobalTextId(unit.DescriptionText),
            new GlobalTextId(unit.AbilityText),
            unit.Hitpoints,
            unit.Armor,
            unit.Regen,
            attack,
            secondaryAttack,
            unit.AttackTwice == true,
            ConvertUnitCategory(unit.UnitCategory),
            ConvertLeaderCategory(unit.LeaderCategory),
            ConvertRace(race.RaceType),
            ConvertSubrace(unit.SubraceType),
            unit.IsMale == true,
            ConvertDeathAnimation(unit.DeathAnimation),
            unit.SizeSmall != true,
            unit.WaterOnly == true,
            moveAbilities.GetValueOrDefault(id),
            unit.Scout,
            ConvertResourceSet(unit.EnrollCost),
            ConvertResourceSet(unit.ReviveCost),
            ConvertResourceSet(unit.HealCost),
            ConvertResourceSet(unit.TrainingCost),
            ConvertOptionalId(unit.EnrollBuildingId, static id => new WorldPlayerBuildingDefinitionId(id)),
            unit.ExperienceKilled,
            unit.ExperienceNext,
            unit.Level,
            unit.Leadership,
            unit.Negotiate,
            unit.Move,
            unit.Lifetime,
            leaderAbilities.GetValueOrDefault(id),
            ConvertOptionalId(unit.PreviousId, static id => new UnitDefinitionId(id)),
            wards);
    }

    private static IEnumerable<UnitAttackDefinition> LoadAttacks(GlobalDatabase globalDb)
    {
        foreach (Db.Attack attack in globalDb.Attacks.Values)
        {
            yield return ConvertAttack(globalDb, attack);
        }
    }

    private static IEnumerable<UnitDynamicUpgradeDefinition> LoadDynamicUpgrades(GlobalDatabase globalDb)
    {
        foreach (Db.Unit unit in globalDb.Units.Values)
        {
            UnitDynamicUpgrade lowLevelUpgrade = GetDynamicUpgrade(globalDb, unit.DynamicUpgrade1Id);
            UnitDynamicUpgrade highLevelUpgrade = GetDynamicUpgrade(globalDb, unit.DynamicUpgrade2Id);

            yield return new UnitDynamicUpgradeDefinition(
                new UnitDefinitionId(unit.UnitId),
                unit.DynamicUpgradeLevel,
                lowLevelUpgrade,
                highLevelUpgrade);
        }
    }

    private static IEnumerable<UnitModifierDefinition> LoadUnitModifiers(GlobalDatabase globalDb)
    {
        foreach (Db.UnitModifier unitModifier in globalDb.UnitModifiers.Values)
        {
            yield return CreateUnitModifier(globalDb, new UnitModifierDefinitionId(unitModifier.ModifierId));
        }
    }

    private static UnitModifierDefinition CreateUnitModifier(GlobalDatabase globalDb, UnitModifierDefinitionId id)
    {
        Db.Id globalModifierId = new(id.Value);

        IReadOnlyList<Db.UnitModifierItem> items = [.. globalDb.UnitModifierItemsByBelongsToId[globalModifierId]];

        GlobalTextId descriptionTextId = items
            .Select(item => new GlobalTextId(item.Description))
            .Distinct()
            .Single();

        UnitModifierDefinition modifier = new(
            id,
            descriptionTextId,
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: default,
            Hitpoints: default,
            RegenerationPercent: default,
            Armor: default,
            Initiative: default,
            Damage: default,
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.None,
            Wards: []);

        foreach (Db.UnitModifierItem item in items)
        {
            modifier = item.Type switch
            {
                Db.UnitModifierItemType.MoveAbility => ApplyMovement(modifier, item.Move),
                Db.UnitModifierItemType.Hitpoints => modifier with { Hitpoints = AddHitpoints(modifier.Hitpoints, item) },
                Db.UnitModifierItemType.Armor => modifier with { Armor = AddStat(modifier.Armor, item) },
                Db.UnitModifierItemType.Initiative => modifier with { Initiative = AddStat(modifier.Initiative, item) },
                Db.UnitModifierItemType.QuantityDamage => modifier with { Damage = AddStat(modifier.Damage, item) },
                Db.UnitModifierItemType.Power => modifier with { Accuracy = AddStat(modifier.Accuracy, item) },
                Db.UnitModifierItemType.QuantityTarget => modifier,
                Db.UnitModifierItemType.VisionRadius => modifier with { VisionRadius = AddStat(modifier.VisionRadius, item) },
                Db.UnitModifierItemType.Leadership => modifier with { Leadership = AddStat(modifier.Leadership, item) },
                Db.UnitModifierItemType.MoveAllowance => modifier with { MaxMovement = AddStat(modifier.MaxMovement, item) },
                Db.UnitModifierItemType.LeaderAbility => AddLeaderAbility(modifier, item),
                Db.UnitModifierItemType.Immunity => AddSourceWard(modifier, item),
                Db.UnitModifierItemType.Regeneration => modifier with { RegenerationPercent = item.Percent ?? 0 },
                Db.UnitModifierItemType.ImmunityClass => AddClassWard(modifier, item),
                Db.UnitModifierItemType.AttackDrain => modifier with { DrainPercent = modifier.DrainPercent + (item.Percent ?? 0) },
                Db.UnitModifierItemType.FastRetreat => modifier with { FastRetreat = true },
                Db.UnitModifierItemType.LowerCost => AddPurchaseDiscount(modifier, item)
            };
        }

        return modifier;

        static UnitModifierDefinition ApplyMovement(UnitModifierDefinition modifier, Db.GroundType? move)
        {
            if (move is not null)
            {
                return modifier with
                {
                    MoveAbility = modifier.MoveAbility | ConvertMoveAbility(move.Value)
                };
            }
            else
            {
                return modifier;
            }
        }

        static UnitStatModifier AddStat(UnitStatModifier modifier, Db.UnitModifierItem item)
        {
            return new UnitStatModifier(modifier.Flat + (item.Number ?? 0), modifier.Percent + (item.Percent ?? 0));
        }

        static UnitStatModifier AddHitpoints(UnitStatModifier modifier, Db.UnitModifierItem item)
        {
            if (item.Percent is null || item.Number is null)
            {
                throw new InvalidDataException("Unit modifier hitpoints is incomplete.");
            }

            bool isPercent = item.Number == 1;
            if (isPercent)
            {
                return modifier with { Percent = modifier.Percent + item.Percent.Value };
            }
            else
            {
                return modifier with { Flat = modifier.Flat + item.Percent.Value };
            }
        }

        static UnitModifierDefinition AddLeaderAbility(UnitModifierDefinition modifier, Db.UnitModifierItem item)
        {
            if (item.Ability is null)
            {
                throw new InvalidDataException("Unit modifier leader ability is incomplete.");
            }

            return modifier with
            {
                LeaderAbilities = modifier.LeaderAbilities | ConvertLeaderAbility(item.Ability.Value)
            };
        }

        static UnitModifierDefinition AddSourceWard(UnitModifierDefinition modifier, Db.UnitModifierItem item)
        {
            if (item.Immunity is null || item.ImmunityType is null)
            {
                throw new InvalidDataException("Unit modifier source immunity is incomplete.");
            }

            UnitWard ward = new UnitAttackSourceWard(
                ConvertAttackSource(item.Immunity.Value),
                ConvertWardCategory(item.ImmunityType.Value));

            return modifier with { Wards = modifier.Wards.Add(ward) };
        }

        static UnitModifierDefinition AddClassWard(UnitModifierDefinition modifier, Db.UnitModifierItem item)
        {
            if (item.Immunity is null || item.ImmunityType is null)
            {
                throw new InvalidDataException("Unit modifier attack class immunity is incomplete.");
            }

            Db.AttackType attackType = (Db.AttackType)(int)item.Immunity.Value;
            UnitWard ward = new UnitAttackClassWard(
                ConvertAttackType(attackType),
                ConvertWardCategory(item.ImmunityType.Value));

            return modifier with { Wards = modifier.Wards.Add(ward) };
        }

        static UnitModifierDefinition AddPurchaseDiscount(UnitModifierDefinition modifier, Db.UnitModifierItem item)
        {
            if (item.Percent is null)
            {
                throw new InvalidDataException("Unit modifier lower cost is incomplete.");
            }

            return modifier with
            {
                PurchaseDiscountPercent = modifier.PurchaseDiscountPercent + item.Percent.Value
            };
        }
    }

    private static IEnumerable<ItemDefinition> LoadItems(GlobalDatabase globalDb)
    {
        foreach (Db.Item globalItem in globalDb.Items.Values)
        {
            yield return new ItemDefinition(
                new ItemDefinitionId(globalItem.ItemId),
                new GlobalTextId(globalItem.NameText),
                new GlobalTextId(globalItem.DescriptionText),
                ConvertItemCategory(globalItem.ItemCategory),
                ConvertResourceSet(globalItem.Value),
                ConvertOptionalId(globalItem.ModifierEquipment, static id => new UnitModifierDefinitionId(id)),
                ConvertEquipmentAttackId(globalItem),
                ConvertItemEffect(globalItem),
                ConvertOptionalId(globalItem.UnitId, static id => new UnitDefinitionId(id)));
        }
    }

    private static IEnumerable<WorldLandmarkDefinition> LoadLandmarks(GlobalDatabase globalDb)
    {
        foreach (Db.Landmark landmark in globalDb.Landmarks.Values)
        {
            yield return new WorldLandmarkDefinition(
                new WorldLandmarkDefinitionId(landmark.LandmarkId),
                new GlobalTextId(landmark.NameText),
                new GlobalTextId(landmark.DescriptionText),
                ConvertLandmarkType(landmark.LandmarkType),
                landmark.Mountain ?? false,
                landmark.Cx,
                landmark.Cy);
        }
    }

    private static IEnumerable<WorldPlayerBuildingDefinition> LoadPlayerBuildings(GlobalDatabase globalDb)
    {
        ILookup<WorldPlayerBuildingDefinitionId, UnitDefinitionId> units = LoadBuildingUnits(globalDb);

        foreach (Db.Building building in globalDb.Buildings.Values)
        {
            WorldPlayerBuildingDefinitionId id = new(building.BuildingId);

            yield return new WorldPlayerBuildingDefinition(
                id,
                new GlobalTextId(building.NameText),
                new GlobalTextId(building.DescriptionText),
                ConvertOptionalId(building.RequiredId, static id => new WorldPlayerBuildingDefinitionId(id)),
                ConvertBuildingType(building.Category),
                ConvertUnitBranch(building.Branch),
                building.Level,
                ConvertResourceSet(building.Cost),
                [.. units[id]]);
        }
    }

    private static IEnumerable<WorldThiefActionDefinition> LoadThiefActions(GlobalDatabase globalDb)
    {
        foreach (Db.Action action in globalDb.Actions.Values)
        {
            yield return new WorldThiefActionDefinition(
                ConvertThiefActionType(action.Category),
                action.Chance,
                new GlobalTextId(action.NameText),
                new GlobalTextId(action.DescriptionText));
        }
    }

    private static IEnumerable<WorldSpellDefinition> LoadSpells(GlobalDatabase globalDb)
    {
        Dictionary<WorldSpellDefinitionId, WorldRace> spellRaces = [];

        foreach (WorldSpellResearchCost research in LoadSpellResearchCosts(globalDb))
        {
            spellRaces.TryAdd(research.SpellId, research.Race);
        }

        foreach (Db.Spell spell in globalDb.Spells.Values)
        {
            WorldSpellDefinitionId spellId = new(spell.SpellId);

            yield return new WorldSpellDefinition(
                spellId,
                new GlobalTextId(spell.NameText),
                new GlobalTextId(spell.DescriptionText),
                new GlobalTextId(spell.ModifierText),
                spellRaces.GetValueOrDefault(spellId, WorldRace.Human),
                spell.Level,
                ConvertResourceSet(spell.CastingCost),
                ConvertResourceSet(spell.BuyCost),
                ConvertSpellAreaToRadius(spell),
                ConvertSpellEffect(spell),
                ConvertAiSpellType(spell.AiCategory));
        }
    }

    private static UnitAttackDefinitionId? ConvertEquipmentAttackId(Db.Item item)
    {
        return item.ItemCategory switch
        {
            Db.MagicItemType.Armor => null,
            Db.MagicItemType.Banner => null,
            Db.MagicItemType.Jewel => null,
            Db.MagicItemType.Orb => null,
            Db.MagicItemType.PotionBoost => null,
            Db.MagicItemType.PotionHeal => null,
            Db.MagicItemType.PotionPermanent => null,
            Db.MagicItemType.PotionRevive => null,
            Db.MagicItemType.Scroll => null,
            Db.MagicItemType.Special => null,
            Db.MagicItemType.Talisman => null,
            Db.MagicItemType.TravelItem => null,
            Db.MagicItemType.Valuable => null,
            Db.MagicItemType.Wand => null,
            Db.MagicItemType.Weapon => ConvertOptionalId(item.AttackId, static id => new UnitAttackDefinitionId(id))
        };
    }

    private static ItemEffect? ConvertItemEffect(Db.Item item)
    {
        return item.ItemCategory switch
        {
            Db.MagicItemType.Armor => null,
            Db.MagicItemType.Banner => null,
            Db.MagicItemType.Jewel => null,
            Db.MagicItemType.Orb => new AttackItemEffect(new UnitAttackDefinitionId(RequiredField(item.AttackId).Value)),
            Db.MagicItemType.PotionBoost => new TemporaryBoostItemEffect(new UnitModifierDefinitionId(RequiredField(item.ModifierPotion).Value)),
            Db.MagicItemType.PotionHeal => new HealItemEffect(RequiredField(item.HitpointsPotion)),
            Db.MagicItemType.PotionPermanent => new PermanentBoostItemEffect(new UnitModifierDefinitionId(RequiredField(item.ModifierPotion).Value)),
            Db.MagicItemType.PotionRevive => new ReviveItemEffect(),
            Db.MagicItemType.Scroll => ConvertSpellItemEffect(item),
            Db.MagicItemType.Special => null,
            Db.MagicItemType.Talisman => new AttackItemEffect(new UnitAttackDefinitionId(RequiredField(item.AttackId).Value)),
            Db.MagicItemType.TravelItem => null,
            Db.MagicItemType.Valuable => null,
            Db.MagicItemType.Wand => ConvertSpellItemEffect(item),
            Db.MagicItemType.Weapon => null
        };
    }

    private static SpellItemEffect ConvertSpellItemEffect(Db.Item item)
    {
        WorldResourceSet castingCost = WorldResourceSet.Empty;

        if (item.Casting is { } casting)
        {
            castingCost = ConvertResourceSet(casting);
        }

        return new SpellItemEffect(new WorldSpellDefinitionId(RequiredField(item.SpellId).Value), castingCost);
    }

    private static WorldAbstractSpellEffect ConvertSpellEffect(Db.Spell spell)
    {
        return spell.Category switch
        {
            Db.SpellType.Attack => new WorldAttackSpellEffect(ConvertAttackSource(RequiredField(spell.DamageSource)), RequiredField(spell.DamageQuantity)),
            Db.SpellType.Lower => new WorldWeakenSpellEffect(new UnitModifierDefinitionId(RequiredField(spell.ModifierId).Value)),
            Db.SpellType.Heal => new WorldHealSpellEffect(RequiredField(spell.HealQuantity)),
            Db.SpellType.Boost => new WorldBoostSpellEffect(new UnitModifierDefinitionId(RequiredField(spell.ModifierId).Value)),
            Db.SpellType.Summon => new WorldSummonStackSpellEffect(new UnitDefinitionId(RequiredField(spell.UnitId).Value)),
            Db.SpellType.Fog => new WorldFogSpellEffect(),
            Db.SpellType.Unfog => new WorldUnfogSpellEffect(),
            Db.SpellType.RestoreMovement => new WorldRestoreMovementSpellEffect(RequiredField(spell.RestoreMovement)),
            Db.SpellType.Invisibility => new WorldInvisibilitySpellEffect(),
            Db.SpellType.RemoveRod => new WorldRemoveRodSpellEffect(),
            Db.SpellType.ChangeTerrain => new WorldChangeTerrainSpellEffect(MapGroundFeature(RequiredField(spell.GroundCategory))),
            Db.SpellType.GiveWards => throw new NotSupportedException($"Spell '{spell.SpellId}' has an unsupported category.")
        };

        WorldMapFeature MapGroundFeature(Db.GroundType ground)
        {
            return ground switch
            {
                Db.GroundType.Plain => WorldMapFeature.None,
                Db.GroundType.Forest => WorldMapFeature.Forest,
                Db.GroundType.Mountain => WorldMapFeature.Mountain,
                Db.GroundType.Water => throw new NotSupportedException($"Spell '{spell.SpellId}' has an unsupported ground type.")
            };
        }
    }

    private static WorldAiSpellType ConvertAiSpellType(Db.AiSpellType type)
    {
        return type switch
        {
            Db.AiSpellType.All => WorldAiSpellType.All,
            Db.AiSpellType.None => WorldAiSpellType.None,
            Db.AiSpellType.Human => WorldAiSpellType.Human,
            Db.AiSpellType.Undead => WorldAiSpellType.Undead,
            Db.AiSpellType.Heretic => WorldAiSpellType.Heretic,
            Db.AiSpellType.Dwarf => WorldAiSpellType.Dwarf
        };
    }

    private static int? ConvertSpellAreaToRadius(Db.Spell spell)
    {
        if (spell.Area < 999)
        {
            if (spell.Area < 1 || spell.Area % 2 == 0)
            {
                throw new InvalidDataException($"Spell '{spell.SpellId}' has an unexpected area value '{spell.Area}'.");
            }

            return (spell.Area - 1) / 2;
        }
        else
        {
            return null;
        }
    }

    private static UnitDynamicUpgrade GetDynamicUpgrade(GlobalDatabase globalDb, Db.Id upgradeId)
    {
        if (!globalDb.UnitDynamicUpgrades.TryGetValue(upgradeId, out Db.UnitDynamicUpgrade? upgrade))
        {
            throw new InvalidDataException($"Dynamic upgrade definition '{upgradeId}' was not found.");
        }

        return new UnitDynamicUpgrade(
            upgrade.HitPoint,
            upgrade.Armor ?? 0,
            upgrade.Regen ?? 0,
            upgrade.Initiative ?? 0,
            upgrade.Damage ?? 0,
            upgrade.Heal ?? 0,
            upgrade.Power ?? 0,
            upgrade.ExperienceKilled,
            upgrade.ExperienceNext,
            upgrade.Move ?? 0,
            upgrade.Negotiate,
            ConvertResourceSet(upgrade.EnrollCost),
            ConvertResourceSet(upgrade.ReviveCost),
            ConvertResourceSet(upgrade.HealCost),
            ConvertResourceSet(upgrade.TrainingCost));
    }

    private static FrozenDictionary<UnitDefinitionId, ImmutableArray<UnitUpgradeDefinition>> LoadUnitUpgrades(GlobalDatabase globalDb)
    {
        Dictionary<UnitDefinitionId, ImmutableArray<UnitUpgradeDefinition>.Builder> upgrades = [];

        foreach (Db.Unit unit in globalDb.Units.Values)
        {
            if (unit.PreviousId is null)
            {
                continue;
            }

            UnitDefinitionId fromUnitId = new(unit.PreviousId.Value);

            if (!upgrades.TryGetValue(fromUnitId, out ImmutableArray<UnitUpgradeDefinition>.Builder? unitUpgradeBuilder))
            {
                unitUpgradeBuilder = ImmutableArray.CreateBuilder<UnitUpgradeDefinition>();
                upgrades[fromUnitId] = unitUpgradeBuilder;
            }

            unitUpgradeBuilder.Add(new UnitUpgradeDefinition(
                fromUnitId,
                new UnitDefinitionId(unit.UnitId),
                ConvertOptionalId(unit.UpgradeBuildingId, static id => new WorldPlayerBuildingDefinitionId(id))));
        }

        return upgrades.ToFrozenDictionary(entry => entry.Key, entry => entry.Value.ToImmutable());
    }

    private static ILookup<WorldPlayerBuildingDefinitionId, UnitDefinitionId> LoadBuildingUnits(GlobalDatabase globalDb)
    {
        List<(WorldPlayerBuildingDefinitionId BuildingId, UnitDefinitionId UnitId)> entries = [];

        foreach (Db.Unit unit in globalDb.Units.Values)
        {
            if (unit.BaseUnit is null)
            {
                UnitDefinitionId unitDefinitionId = new(unit.UnitId);

                if (unit.UpgradeBuildingId is not null)
                {
                    entries.Add((new WorldPlayerBuildingDefinitionId(unit.UpgradeBuildingId.Value), unitDefinitionId));
                }

                if (unit.EnrollBuildingId is not null && unit.EnrollBuildingId != unit.UpgradeBuildingId)
                {
                    entries.Add((new WorldPlayerBuildingDefinitionId(unit.EnrollBuildingId.Value), unitDefinitionId));
                }
            }
        }

        return entries.ToLookup(unit => unit.BuildingId, unit => unit.UnitId);
    }

    private static FrozenDictionary<UnitDefinitionId, LeaderAbility> LoadLeaderAbilities(GlobalDatabase globalDb)
    {
        Dictionary<UnitDefinitionId, LeaderAbility> result = [];

        foreach (IGrouping<Db.Id, Db.LeaderAbility> entry in globalDb.LeaderAbilitiesByUnitId)
        {
            LeaderAbility abilities = LeaderAbility.None;

            foreach (Db.LeaderAbility ability in entry)
            {
                abilities |= ConvertLeaderAbility(ability.Ability);
            }

            result[new UnitDefinitionId(entry.Key)] = abilities;
        }

        return result.ToFrozenDictionary();
    }

    private static int CompareLeaderUpgrade(LeaderUpgradeDefinition left, LeaderUpgradeDefinition right)
    {
        int rv;

        rv = left.MinLevel.CompareTo(right.MinLevel);
        if (rv != 0)
        {
            return rv;
        }

        rv = right.Priority.CompareTo(left.Priority);
        if (rv != 0)
        {
            return rv;
        }

        rv = left.ModifierId.CompareTo(right.ModifierId);
        if (rv != 0)
        {
            return rv;
        }

        return 0;
    }

    private static FrozenDictionary<LordDefinitionId, ImmutableArray<LeaderUpgradeDefinition>> LoadLeaderUpgrades(GlobalDatabase globalDb)
    {
        Dictionary<LordDefinitionId, ImmutableArray<LeaderUpgradeDefinition>> upgrades = [];

        foreach (IGrouping<Db.Id, Db.LeaderUpgrade> entry in globalDb.LeaderUpgradesByBelongsToId)
        {
            LordDefinitionId lordId = new(entry.Key);

            List<LeaderUpgradeDefinition> definitions = [];

            foreach (Db.LeaderUpgrade upgrade in entry)
            {
                definitions.Add(new LeaderUpgradeDefinition(
                    lordId,
                    new UnitModifierDefinitionId(upgrade.ModifierId),
                    upgrade.MinLevel,
                    upgrade.Priority,
                    new GlobalTextId(upgrade.NameText),
                    new GlobalTextId(upgrade.DescriptionText)));
            }

            definitions.Sort(CompareLeaderUpgrade);

            upgrades.Add(lordId, [.. definitions]);
        }

        return upgrades.ToFrozenDictionary();
    }

    private static IEnumerable<LordDefinition> LoadLords(GlobalDatabase globalDb)
    {
        foreach (Db.Lord lord in globalDb.Lords.Values)
        {
            if (!globalDb.Races.TryGetValue(lord.RaceId, out Db.Race? raceInfo))
            {
                continue;
            }

            ImmutableArray<WorldPlayerBuildingDefinitionId>.Builder buildingIds = ImmutableArray.CreateBuilder<WorldPlayerBuildingDefinitionId>();

            foreach (Db.BuildingList entry in globalDb.BuildingLists)
            {
                if (entry.LordId == lord.LordId)
                {
                    buildingIds.Add(new WorldPlayerBuildingDefinitionId(entry.BuildingId));
                }
            }

            yield return new LordDefinition(
                new LordDefinitionId(lord.LordId),
                ConvertRace(raceInfo.RaceType),
                ConvertLordCategory(lord.Category),
                new GlobalTextId(lord.NameText),
                new GlobalTextId(lord.DescriptionText),
                buildingIds.ToImmutable());
        }
    }

    private static FrozenDictionary<UnitDefinitionId, UnitMoveAbility> LoadMoveAbilities(GlobalDatabase globalDb)
    {
        Dictionary<UnitDefinitionId, UnitMoveAbility> moveAbilities = [];

        foreach (IGrouping<Db.Id, Db.MoveAbility> entry in globalDb.MoveAbilitiesByUnitId)
        {
            UnitMoveAbility ability = UnitMoveAbility.None;

            foreach (Db.MoveAbility moveAbility in entry)
            {
                ability |= ConvertMoveAbility(moveAbility.Ability);
            }

            moveAbilities[new UnitDefinitionId(entry.Key)] = ability;
        }

        return moveAbilities.ToFrozenDictionary();
    }

    private static UnitMoveAbility ConvertMoveAbility(Db.GroundType ground)
    {
        return ground switch
        {
            Db.GroundType.Forest => UnitMoveAbility.Forest,
            Db.GroundType.Water => UnitMoveAbility.Water,
            Db.GroundType.Plain => UnitMoveAbility.None,
            Db.GroundType.Mountain => UnitMoveAbility.None
        };
    }

    private static FrozenDictionary<UnitDefinitionId, ImmutableArray<UnitWard>> LoadUnitWards(GlobalDatabase globalDb)
    {
        Dictionary<UnitDefinitionId, ImmutableArray<UnitWard>.Builder> wards = [];

        foreach (Db.UnitAttackImmunity immunity in globalDb.UnitAttackImmunities)
        {
            AddWard(
                new UnitDefinitionId(immunity.UnitId),
                new UnitAttackClassWard(ConvertAttackType(immunity.Immunity), ConvertWardCategory(immunity.ImmunityType)));
        }

        foreach (Db.UnitAttackSourceImmunity immunity in globalDb.UnitAttackSourceImmunities)
        {
            AddWard(
                new UnitDefinitionId(immunity.UnitId),
                new UnitAttackSourceWard(ConvertAttackSource(immunity.Immunity), ConvertWardCategory(immunity.ImmunityType)));
        }

        return wards.ToFrozenDictionary(entry => entry.Key, entry => entry.Value.ToImmutable());

        void AddWard(UnitDefinitionId unitDefinitionId, UnitWard ward)
        {
            if (!wards.TryGetValue(unitDefinitionId, out ImmutableArray<UnitWard>.Builder? unitWardBuilder))
            {
                unitWardBuilder = ImmutableArray.CreateBuilder<UnitWard>();
                wards[unitDefinitionId] = unitWardBuilder;
            }

            unitWardBuilder.Add(ward);
        }
    }

    private static IEnumerable<WorldCityDefinition> LoadCities(GlobalDatabase globalDb)
    {
        foreach (Db.CityInfo cityInfo in globalDb.CityInfos)
        {
            if (globalDb.Races.TryGetValue(cityInfo.RaceId, out Db.Race? raceInfo))
            {
                WorldRace race = ConvertRace(raceInfo.RaceType);

                yield return new WorldCityDefinition(
                    new WorldCityDefinitionId(race, cityInfo.Size),
                    cityInfo.Size,
                    cityInfo.Scout,
                    cityInfo.Regeneration,
                    ConvertResourceSet(cityInfo.Growth),
                    new UnitModifierDefinitionId(cityInfo.ProtectionId));
            }
        }
    }

    private static IEnumerable<WorldRaceDefinition> LoadRaces(GlobalDatabase globalDb, IEnumerable<WorldCityDefinition> cities)
    {
        ILookup<WorldRace, WorldCityDefinition> citiesByRace = cities.ToLookup(city => city.Id.Race);
        ILookup<Db.Id, Db.TextLeader> leaderNamesByRace = globalDb.TextLeaders.ToLookup(name => name.RaceId);

        foreach (Db.Race raceInfo in globalDb.Races.Values)
        {
            WorldRace race = ConvertRace(raceInfo.RaceType);
            WorldResourceSet income = ConvertResourceSet(raceInfo.Income);
            WorldMapTerrain nativeTerrain = WorldHelpers.GetNativeTerrain(race);
            IEnumerable<UnitDefinitionId> leaderDefinitions = GetRaceLeaders(raceInfo);
            IEnumerable<UnitDefinitionId> unitDefinitions = GetRaceUnits(raceInfo);

            List<WorldCityDefinitionId> cityDefinitions = [];

            foreach (WorldCityDefinition city in citiesByRace[race])
            {
                cityDefinitions.Add(city.Id);
            }

            List<string> maleLeaderNames = [];
            List<string> femaleLeaderNames = [];

            foreach (Db.TextLeader leaderName in leaderNamesByRace[raceInfo.RaceId])
            {
                if (leaderName.IsMale == true)
                {
                    maleLeaderNames.Add(leaderName.Text);
                }
                else
                {
                    femaleLeaderNames.Add(leaderName.Text);
                }
            }

            yield return new WorldRaceDefinition(
                race,
                new GlobalTextId(raceInfo.NameText),
                income,
                nativeTerrain,
                raceInfo.Playable,
                raceInfo.Regeneration ?? 0,
                ConvertOptionalId(raceInfo.ProtectionId, static id => new UnitModifierDefinitionId(id)),
                [.. cityDefinitions],
                [.. leaderDefinitions],
                [.. unitDefinitions],
                [.. maleLeaderNames],
                [.. femaleLeaderNames]);
        }
    }

    private static IEnumerable<WorldSpellResearchCost> LoadSpellResearchCosts(GlobalDatabase globalDb)
    {
        foreach (Db.SpellResearch research in globalDb.SpellResearches)
        {
            if (!globalDb.Lords.TryGetValue(research.LordId, out Db.Lord? lord))
            {
                throw new InvalidDataException($"Lord identifier '{research.LordId}' for spell '{research.SpellId}' was not found.");
            }

            if (!globalDb.Races.TryGetValue(lord.RaceId, out Db.Race? raceInfo))
            {
                throw new InvalidDataException($"Race identifier '{lord.RaceId}' for lord '{lord.LordId}' was not found.");
            }

            WorldRace race = ConvertRace(raceInfo.RaceType);

            yield return new WorldSpellResearchCost(
                race,
                new WorldSpellDefinitionId(research.SpellId),
                ConvertResourceSet(research.Research));
        }
    }

    private static Db.Attack GetAttack(GlobalDatabase globalDb, Db.Id attackId, UnitDefinitionId unitDefinitionId)
    {
        if (!globalDb.Attacks.TryGetValue(attackId, out Db.Attack? attack))
        {
            throw new InvalidDataException($"Attack definition '{attackId}' for unit definition '{unitDefinitionId}' was not found.");
        }

        return attack;
    }

    private static UnitAttackDefinition ConvertAttack(GlobalDatabase globalDb, Db.Attack attack)
    {
        UnitAttackType type = ConvertAttackType(attack.Type);

        ImmutableArray<UnitDefinitionId> summonUnitIds = [];
        (UnitDefinitionId? Small, UnitDefinitionId? Large) transformUnitIds = (null, null);
        ImmutableArray<UnitModifierDefinitionId> wardModifierIds = [];

        switch (type)
        {
            case UnitAttackType.Summon:
                summonUnitIds = LoadSummonUnitIds(globalDb, attack);
                break;
            case UnitAttackType.TransformSelf:
            case UnitAttackType.TransformEnemy:
                transformUnitIds = LoadTransformUnitIds(globalDb, attack, type);
                break;
            case UnitAttackType.GiveProtection:
                wardModifierIds = LoadWardModifierIds(attack);
                break;
        }

        return new UnitAttackDefinition(
            new UnitAttackDefinitionId(attack.AttackId.Value),
            type,
            ConvertAttackSource(attack.Source),
            attack.Initiative,
            attack.Power,
            ConvertAttackReach(attack.Reach),
            attack.QuantityDamage ?? 0,
            attack.QuantityHeal ?? 0,
            attack.Level ?? 0,
            attack.Infinite == true,
            summonUnitIds,
            transformUnitIds.Small,
            transformUnitIds.Large,
            attack.CritHit == true,
            ConvertOptionalId(attack.AltAttackId, static id => new UnitAttackDefinitionId(id)),
            wardModifierIds);
    }

    private static ImmutableArray<UnitDefinitionId> LoadSummonUnitIds(GlobalDatabase globalDb, Db.Attack attack)
    {
        List<UnitDefinitionId> summonUnitIds = [];

        foreach (Db.AttackSummonTransform transform in globalDb.AttackSummonTransformsByAttackId[attack.AttackId])
        {
            summonUnitIds.Add(new UnitDefinitionId(transform.TransformId.Value));
        }

        if (summonUnitIds.Count == 0)
        {
            throw new InvalidDataException($"Attack '{attack.AttackId}' does not define a summoned unit.");
        }

        return [.. summonUnitIds];
    }

    private static ImmutableArray<UnitModifierDefinitionId> LoadWardModifierIds(Db.Attack attack)
    {
        ImmutableArray<UnitModifierDefinitionId>.Builder wardModifierIds = ImmutableArray.CreateBuilder<UnitModifierDefinitionId>();

        foreach (Db.Id? wardId in (ReadOnlySpan<Db.Id?>)[attack.Ward1, attack.Ward2, attack.Ward3, attack.Ward4])
        {
            if (wardId is not null)
            {
                wardModifierIds.Add(new UnitModifierDefinitionId(wardId.Value));
            }
        }

        if (wardModifierIds.Count <= 0)
        {
            throw new InvalidDataException($"Attack '{attack.AttackId}' does not define protection wards.");
        }

        return wardModifierIds.ToImmutable();
    }

    private static (UnitDefinitionId? Small, UnitDefinitionId? Large) LoadTransformUnitIds(GlobalDatabase globalDb, Db.Attack attack, UnitAttackType type)
    {
        UnitDefinitionId? smallId = null;
        UnitDefinitionId? largeId = null;

        foreach (Db.AttackSummonTransform transform in globalDb.AttackSummonTransformsByAttackId[attack.AttackId])
        {
            if (!globalDb.Units.TryGetValue(transform.TransformId, out Db.Unit? unit))
            {
                throw new InvalidDataException($"Attack '{attack.AttackId}' references missing transformed unit '{transform.TransformId}'.");
            }

            UnitDefinitionId transformUnitId = new(transform.TransformId.Value);

            if (unit.SizeSmall == true)
            {
                if (smallId is not null)
                {
                    throw new InvalidDataException($"Attack '{attack.AttackId}' defines multiple small transformed units.");
                }

                smallId = transformUnitId;
            }
            else
            {
                if (largeId is not null)
                {
                    throw new InvalidDataException($"Attack '{attack.AttackId}' defines multiple large transformed units.");
                }

                largeId = transformUnitId;
            }
        }

        return type switch
        {
            UnitAttackType.TransformEnemy when smallId is null =>
                throw new InvalidDataException($"Attack '{attack.AttackId}' does not define a small transformed unit."),
            UnitAttackType.TransformEnemy when largeId is null =>
                throw new InvalidDataException($"Attack '{attack.AttackId}' does not define a large transformed unit."),
            UnitAttackType.TransformSelf when smallId is null && largeId is null =>
                throw new InvalidDataException($"Attack '{attack.AttackId}' does not define a transformed unit."),
            _ => (smallId, largeId)
        };
    }

    private static UnitAttackType ConvertAttackType(Db.AttackType type)
    {
        return type switch
        {
            Db.AttackType.Damage => UnitAttackType.Damage,
            Db.AttackType.DrainLife => UnitAttackType.DrainLife,
            Db.AttackType.Paralyze => UnitAttackType.Paralyze,
            Db.AttackType.Heal => UnitAttackType.Heal,
            Db.AttackType.Fear => UnitAttackType.Fear,
            Db.AttackType.IncreaseDamage => UnitAttackType.IncreaseDamage,
            Db.AttackType.Petrify => UnitAttackType.Petrify,
            Db.AttackType.ReduceDamage => UnitAttackType.ReduceDamage,
            Db.AttackType.ReduceInitiative => UnitAttackType.ReduceInitiative,
            Db.AttackType.Poison => UnitAttackType.Poison,
            Db.AttackType.Frostbite => UnitAttackType.Frostbite,
            Db.AttackType.Revive => UnitAttackType.Revive,
            Db.AttackType.DrainLifeOverflow => UnitAttackType.DrainLifeOverflow,
            Db.AttackType.Cure => UnitAttackType.Cure,
            Db.AttackType.Summon => UnitAttackType.Summon,
            Db.AttackType.ReduceLevel => UnitAttackType.ReduceLevel,
            Db.AttackType.GiveAdditionalAttack => UnitAttackType.GiveAdditionalAttack,
            Db.AttackType.Doppelganger => UnitAttackType.Doppelganger,
            Db.AttackType.TransformSelf => UnitAttackType.TransformSelf,
            Db.AttackType.TransformEnemy => UnitAttackType.TransformEnemy,
            Db.AttackType.Blister => UnitAttackType.Blister,
            Db.AttackType.GiveProtection => UnitAttackType.GiveProtection,
            Db.AttackType.ReduceArmor => UnitAttackType.ReduceArmor
        };
    }

    private static ItemCategory ConvertItemCategory(Db.MagicItemType category)
    {
        return category switch
        {
            Db.MagicItemType.Armor => ItemCategory.Armor,
            Db.MagicItemType.Jewel => ItemCategory.Jewel,
            Db.MagicItemType.Weapon => ItemCategory.Weapon,
            Db.MagicItemType.Banner => ItemCategory.Banner,
            Db.MagicItemType.PotionBoost => ItemCategory.PotionTemporaryBoost,
            Db.MagicItemType.PotionHeal => ItemCategory.PotionHeal,
            Db.MagicItemType.PotionRevive => ItemCategory.PotionRevive,
            Db.MagicItemType.PotionPermanent => ItemCategory.PotionPermanentBoost,
            Db.MagicItemType.Scroll => ItemCategory.Scroll,
            Db.MagicItemType.Wand => ItemCategory.Wand,
            Db.MagicItemType.Valuable => ItemCategory.Valuable,
            Db.MagicItemType.Orb => ItemCategory.Orb,
            Db.MagicItemType.Talisman => ItemCategory.Talisman,
            Db.MagicItemType.TravelItem => ItemCategory.TravelItem,
            Db.MagicItemType.Special => ItemCategory.Special
        };
    }

    private static UnitAttackSource ConvertAttackSource(Db.AttackSourceType source)
    {
        return source switch
        {
            Db.AttackSourceType.Weapon => UnitAttackSource.Weapon,
            Db.AttackSourceType.Mind => UnitAttackSource.Mind,
            Db.AttackSourceType.Life => UnitAttackSource.Life,
            Db.AttackSourceType.Death => UnitAttackSource.Death,
            Db.AttackSourceType.Fire => UnitAttackSource.Fire,
            Db.AttackSourceType.Water => UnitAttackSource.Water,
            Db.AttackSourceType.Earth => UnitAttackSource.Earth,
            Db.AttackSourceType.Air => UnitAttackSource.Air
        };
    }

    private static UnitWardCategory ConvertWardCategory(Db.ImmunityType type)
    {
        return type switch
        {
            Db.ImmunityType.NotImmune => throw new InvalidDataException("Unexpected unit ward immunity type."),
            Db.ImmunityType.Once => UnitWardCategory.Once,
            Db.ImmunityType.Always => UnitWardCategory.Always
        };
    }

    private static IEnumerable<UnitDefinitionId> GetRaceLeaders(Db.Race raceInfo)
    {
        List<UnitDefinitionId> leaders = [];

        AddIfPresent(raceInfo.Leader1Id);
        AddIfPresent(raceInfo.Leader2Id);
        AddIfPresent(raceInfo.Leader3Id);
        AddIfPresent(raceInfo.Leader4Id);
        AddIfPresent(raceInfo.NobleId);

        return leaders;

        void AddIfPresent(Db.Id? id)
        {
            if (id is not null)
            {
                leaders.Add(new UnitDefinitionId(id.Value));
            }
        }
    }

    private static IEnumerable<UnitDefinitionId> GetRaceUnits(Db.Race raceInfo)
    {
        List<UnitDefinitionId> units = [];

        AddIfPresent(raceInfo.Soldier1Id);
        AddIfPresent(raceInfo.Soldier2Id);
        AddIfPresent(raceInfo.Soldier3Id);
        AddIfPresent(raceInfo.Soldier4Id);
        AddIfPresent(raceInfo.Soldier5Id);

        return units;

        void AddIfPresent(Db.Id? id)
        {
            if (id is not null)
            {
                units.Add(new UnitDefinitionId(id.Value));
            }
        }
    }

    private static T RequiredField<T>(T? field) where T : struct
    {
        if (field is not null)
        {
            return field.Value;
        }

        throw new InvalidDataException("Required game data field is missing.");
    }

    private static T? ConvertOptionalId<T>(Db.Id? id, Func<Db.Id, T> createFunc) where T : struct
    {
        if (id is null)
        {
            return null;
        }
        else
        {
            return createFunc(id.Value);
        }
    }

    private static WorldRace ConvertRace(Db.RaceType race)
    {
        return race switch
        {
            Db.RaceType.Human => WorldRace.Human,
            Db.RaceType.Undead => WorldRace.Undead,
            Db.RaceType.Heretic => WorldRace.Heretic,
            Db.RaceType.Dwarf => WorldRace.Dwarf,
            Db.RaceType.Neutral => WorldRace.Neutral,
            Db.RaceType.Elf => WorldRace.Elf
        };
    }

    private static UnitAttackReach ConvertAttackReach(Db.AttackReachType reach)
    {
        return reach switch
        {
            Db.AttackReachType.All => UnitAttackReach.All,
            Db.AttackReachType.Any => UnitAttackReach.Any,
            Db.AttackReachType.Adjacent => UnitAttackReach.Adjacent
        };
    }

    private static UnitCategory ConvertUnitCategory(Db.UnitCategoryType category)
    {
        return category switch
        {
            Db.UnitCategoryType.Soldier => UnitCategory.Soldier,
            Db.UnitCategoryType.Noble => UnitCategory.Thief,
            Db.UnitCategoryType.Leader => UnitCategory.Leader,
            Db.UnitCategoryType.Summon => UnitCategory.Summon,
            Db.UnitCategoryType.Illusion => UnitCategory.Illusion,
            Db.UnitCategoryType.NeutralLeader => UnitCategory.NeutralLeader,
            Db.UnitCategoryType.NeutralSoldier => UnitCategory.NeutralSoldier,
            Db.UnitCategoryType.Guardian => UnitCategory.Guardian
        };
    }

    private static LordCategory ConvertLordCategory(Db.LordType lordType)
    {
        return lordType switch
        {
            Db.LordType.Mage => LordCategory.Mage,
            Db.LordType.Warrior => LordCategory.Warrior,
            Db.LordType.Diplomat => LordCategory.Guildmaster
        };
    }

    private static UnitLeaderCategory? ConvertLeaderCategory(Db.LeaderType? leaderType)
    {
        return leaderType switch
        {
            Db.LeaderType.Fighter => UnitLeaderCategory.Fighter,
            Db.LeaderType.Explorer => UnitLeaderCategory.Explorer,
            Db.LeaderType.Mage => UnitLeaderCategory.Mage,
            Db.LeaderType.Rod => UnitLeaderCategory.Rod,
            Db.LeaderType.Noble => UnitLeaderCategory.Thief,
            null => null
        };
    }

    private static WorldSubraceType ConvertSubrace(Db.SubraceType subrace)
    {
        return subrace switch
        {
            Db.SubraceType.Custom => WorldSubraceType.Custom,
            Db.SubraceType.Human => WorldSubraceType.Human,
            Db.SubraceType.Undead => WorldSubraceType.Undead,
            Db.SubraceType.Heretic => WorldSubraceType.Heretic,
            Db.SubraceType.Dwarf => WorldSubraceType.Dwarf,
            Db.SubraceType.Neutral => WorldSubraceType.Neutral,
            Db.SubraceType.NeutralHuman => WorldSubraceType.NeutralHuman,
            Db.SubraceType.NeutralElf => WorldSubraceType.NeutralElf,
            Db.SubraceType.NeutralGreenSkin => WorldSubraceType.NeutralGreenSkin,
            Db.SubraceType.NeutralDragon => WorldSubraceType.NeutralDragon,
            Db.SubraceType.NeutralMarsh => WorldSubraceType.NeutralMarsh,
            Db.SubraceType.NeutralWater => WorldSubraceType.NeutralWater,
            Db.SubraceType.NeutralBarbarian => WorldSubraceType.NeutralBarbarian,
            Db.SubraceType.NeutralWolf => WorldSubraceType.NeutralWolf,
            Db.SubraceType.Elf => WorldSubraceType.Elf
        };
    }

    private static UnitDeathAnimation ConvertDeathAnimation(Db.DeathAnimationType animation)
    {
        return animation switch
        {
            Db.DeathAnimationType.Human => UnitDeathAnimation.Human,
            Db.DeathAnimationType.Heretic => UnitDeathAnimation.Heretic,
            Db.DeathAnimationType.Dwarf => UnitDeathAnimation.Dwarf,
            Db.DeathAnimationType.Undead => UnitDeathAnimation.Undead,
            Db.DeathAnimationType.Neutral => UnitDeathAnimation.Neutral,
            Db.DeathAnimationType.Dragon => UnitDeathAnimation.Dragon,
            Db.DeathAnimationType.Ghost => UnitDeathAnimation.Ghost,
            Db.DeathAnimationType.Elf => UnitDeathAnimation.Elf
        };
    }

    private static WorldThiefActionType ConvertThiefActionType(Db.ActionType type)
    {
        return type switch
        {
            Db.ActionType.PoisonStack => WorldThiefActionType.PoisonStack,
            Db.ActionType.Spy => WorldThiefActionType.Spy,
            Db.ActionType.StealItem => WorldThiefActionType.StealItem,
            Db.ActionType.Assassinate => WorldThiefActionType.Assassinate,
            Db.ActionType.Misfit => WorldThiefActionType.CounterfeitOrders,
            Db.ActionType.Duel => WorldThiefActionType.Duel,
            Db.ActionType.PoisonCity => WorldThiefActionType.PoisonCity,
            Db.ActionType.StealSpell => WorldThiefActionType.StealSpell,
            Db.ActionType.Bribe => WorldThiefActionType.Bribe,
            Db.ActionType.StealGold => WorldThiefActionType.StealGold,
            Db.ActionType.StealMerchant => WorldThiefActionType.StealMerchant,
            Db.ActionType.StealMage => WorldThiefActionType.StealMage,
            Db.ActionType.SpyRuin => WorldThiefActionType.SpyRuin,
            Db.ActionType.RiotCity => WorldThiefActionType.RiotCity
        };
    }

    private static WorldLandmarkType ConvertLandmarkType(Db.LandmarkType type)
    {
        return type switch
        {
            Db.LandmarkType.Misc => WorldLandmarkType.Misc,
            Db.LandmarkType.Building => WorldLandmarkType.Building,
            Db.LandmarkType.Structure => WorldLandmarkType.Structure,
            Db.LandmarkType.Terrain => WorldLandmarkType.Terrain
        };
    }

    private static WorldPlayerBuildingCategory ConvertBuildingType(Db.BuildingType category)
    {
        return category switch
        {
            Db.BuildingType.Guild => WorldPlayerBuildingCategory.Guild,
            Db.BuildingType.Heal => WorldPlayerBuildingCategory.Heal,
            Db.BuildingType.Magic => WorldPlayerBuildingCategory.Spell,
            Db.BuildingType.Unit => WorldPlayerBuildingCategory.Unit
        };
    }

    private static UnitBranch? ConvertUnitBranch(Db.UnitBranchType? branch)
    {
        if (branch is null)
        {
            return null;
        }

        return branch.Value switch
        {
            Db.UnitBranchType.Fighter => UnitBranch.Fighter,
            Db.UnitBranchType.Mage => UnitBranch.Mage,
            Db.UnitBranchType.Archer => UnitBranch.Ranger,
            Db.UnitBranchType.Special => UnitBranch.Special,
            Db.UnitBranchType.Sideshow => UnitBranch.Sideshow,
            Db.UnitBranchType.Hero => UnitBranch.Leader,
            Db.UnitBranchType.Noble => UnitBranch.Leader,
            Db.UnitBranchType.Summon => UnitBranch.Special
        };
    }

    private static WorldResourceSet ConvertResourceSet(Db.ResourceSet resources)
    {
        return new WorldResourceSet(
            resources.Gold,
            resources.DeathMana,
            resources.RuneMana,
            resources.LifeMana,
            resources.InfernalMana,
            resources.GroveMana);
    }

    private static LeaderAbility ConvertLeaderAbility(Db.LeaderAbilityType ability)
    {
        return ability switch
        {
            Db.LeaderAbilityType.Zealot => LeaderAbility.Zealot,
            Db.LeaderAbilityType.Incorruptible => LeaderAbility.Incorruptible,
            Db.LeaderAbilityType.WeaponMaster => LeaderAbility.WeaponMaster,
            Db.LeaderAbilityType.WandScrollUse => LeaderAbility.WandScrollUse,
            Db.LeaderAbilityType.WeaponArmorUse => LeaderAbility.WeaponArmorUse,
            Db.LeaderAbilityType.BannerUse => LeaderAbility.BannerUse,
            Db.LeaderAbilityType.JewelryUse => LeaderAbility.JewelryUse,
            Db.LeaderAbilityType.Rod => LeaderAbility.Rod,
            Db.LeaderAbilityType.OrbUse => LeaderAbility.OrbUse,
            Db.LeaderAbilityType.TalismanUse => LeaderAbility.TalismanUse,
            Db.LeaderAbilityType.TravelItemUse => LeaderAbility.TravelItemUse,
            Db.LeaderAbilityType.CriticalHit => LeaderAbility.CriticalHit
        };
    }
}
