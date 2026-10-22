using System.Text;
using Gallean.Common;

namespace Gallean.Engine.Tests;

internal static class TestHelpers
{
    public static readonly WorldSubrace NeutralSubrace = new(
        new WorldSubraceId("subrace"),
        new WorldPlayerId(1),
        WorldSubraceType.Neutral,
        BannerVariant: new WorldObjectVariant(4));

    public static UnitDefinition SimpleUnitDefinition(string id, int maxHitpoints, int initiative, int damage)
    {
        return new UnitDefinition(
            Id: new UnitDefinitionId(id),
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            AbilityTextId: new GlobalTextId("ability"),
            MaxHitpoints: maxHitpoints,
            Armor: 0,
            Regeneration: 5,
            Attack: SimpleAttack(new UnitAttackDefinitionId(id), initiative, damage, accuracy: 100, reach: UnitAttackReach.Any),
            SecondaryAttack: null,
            AttacksTwice: false,
            Category: UnitCategory.Soldier,
            LeaderCategory: null,
            Race: WorldRace.Human,
            Subrace: WorldSubraceType.Human,
            IsMale: true,
            DeathAnimation: UnitDeathAnimation.Human,
            IsLarge: false,
            IsWaterOnly: false,
            MoveAbility: UnitMoveAbility.None,
            VisionRadius: null,
            PurchaseCost: new WorldResourceSet(50, 0, 0, 0, 0, 0),
            ReviveCost: new WorldResourceSet(50, 0, 0, 0, 0, 0),
            HealCost: new WorldResourceSet(1, 0, 0, 0, 0, 0),
            TrainingCost: new WorldResourceSet(0, 0, 0, 0, 0, 0),
            RequiredBuildingId: null,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000,
            Level: 1,
            Leadership: null,
            Negotiate: null,
            MovementPoints: null,
            Lifetime: null,
            LeaderAbilities: LeaderAbility.None,
            PreviousUnitId: null,
            Wards: []
        );
    }

    public static UnitModifierDefinition SimpleModifier(string id)
    {
        return new UnitModifierDefinition(
            new UnitModifierDefinitionId(id),
            DescriptionTextId: new GlobalTextId("description"),
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
    }

    public static UnitDefinition SimpleLeaderDefinition(UnitDefinition definition, int movementPoints, int leadership)
    {
        return definition with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = leadership,
            MovementPoints = movementPoints
        };
    }

    public static UnitDefinition SimpleThiefDefinition()
    {
        return SimpleUnitDefinition("thief", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Thief,
            Leadership = 1,
            MovementPoints = 20
        };
    }

    public static WorldThiefActionDefinition SimpleThiefActionDefinition(WorldThiefActionType type, int chance)
    {
        return new WorldThiefActionDefinition(
            type,
            chance,
            new GlobalTextId($"{type}-name"),
            new GlobalTextId($"{type}-description"));
    }

    public static WorldUnit SimpleUnit(WorldUnitId id, UnitDefinition definition)
    {
        return new WorldUnit(id, definition.Id, definition.MaxHitpoints, 0, definition.Level, [], null, false);
    }

    public static BattleUnit SimpleBattleUnit(
        BattleUnitId id,
        WorldUnitId? worldUnitId,
        UnitDefinitionId definitionId,
        BattleSide side,
        int hitpoints)
    {
        return new BattleUnit(
            Id: id,
            WorldUnitId: worldUnitId,
            DefinitionId: definitionId,
            Side: side,
            Hitpoints: hitpoints,
            Experience: 0,
            ExperienceGained: 0,
            Level: 1,
            Modifiers: [],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);
    }

    public static UnitGrid<WorldUnit> SingleUnitGrid(WorldUnitId id, UnitDefinition definition)
    {
        return new UnitGrid<WorldUnit>.Builder()
            .Place(
                new WorldUnit(id, definition.Id, definition.MaxHitpoints, 0, definition.Level, [], null, false),
                GridPosition.FrontBottom,
                definition.IsLarge)
            .Build();
    }

    public static UnitGrid<ScenarioStackTemplateUnit> SingleTemplateUnitGrid(UnitDefinition definition)
    {
        return new UnitGrid<ScenarioStackTemplateUnit>.Builder()
            .Place(
                new ScenarioStackTemplateUnit(definition.Id, definition.MaxHitpoints, definition.Level, []),
                GridPosition.FrontBottom,
                definition.IsLarge)
            .Build();
    }

    public static UnitGrid<WorldUnit> StackUnitGrid(
        IGameDefinitionProvider definitions,
        IReadOnlyList<(WorldUnit Unit, GridPosition Position)> units)
    {
        WorldStackBuilder builder = new(definitions);

        foreach ((WorldUnit Unit, GridPosition Position) unitPlacement in units)
        {
            builder.PlaceUnit(unitPlacement.Unit, unitPlacement.Position);
        }

        return builder.Build();
    }

    public static UnitAttackDefinition SimpleAttack(UnitAttackDefinitionId id, int initiative, int damage, int accuracy, UnitAttackReach reach)
    {
        return new UnitAttackDefinition(
            Id: id,
            Type: UnitAttackType.Damage,
            Source: UnitAttackSource.Weapon,
            Initiative: initiative,
            Accuracy: accuracy,
            Reach: reach,
            Damage: damage,
            Heal: 0,
            Level: 0,
            IsInfinite: false,
            SummonUnitIds: [],
            SmallTransformUnitId: null,
            LargeTransformUnitId: null,
            HasCriticalHit: false,
            AlternativeAttackId: null,
            WardModifierIds: []
        );
    }

    public static LordDefinition SimpleLord(LordDefinitionId id, WorldRace race, LordCategory category)
    {
        return new LordDefinition(
            id,
            race,
            category,
            new GlobalTextId("lord"),
            new GlobalTextId("description"),
            BuildingIds: []);
    }

    public static WorldPlayer SimplePlayer(WorldPlayerId id, WorldRace race, WorldMap map)
    {
        return new WorldPlayer(
            Id: id,
            NameTextId: new ScenarioTextId($"Player{id.Value}"),
            LordId: new LordDefinitionId($"Lord{id.Value}"),
            Race: race,
            FaceVariant: new WorldObjectVariant(0),
            AiAttitude: WorldAiAttitudeType.Medium,
            IsAi: false,
            Resources: WorldResourceSet.Empty,
            Buildings: new WorldPlayerBuildings([], []), Spellbook: new WorldPlayerSpellbook([], []),
            FogOfWar: ImmutableBitmap.Create(map.Width, map.Height, initialValue: true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions()
        );
    }

    public static WorldRaceDefinition SimpleRace(WorldRace race, GlobalTextId nameTextId, WorldMapTerrain nativeTerrain)
    {
        return new WorldRaceDefinition(
            Race: race,
            NameTextId: nameTextId,
            Income: WorldResourceSet.Empty,
            NativeTerrain: nativeTerrain,
            IsPlayable: true,
            CapitalRegeneration: 35,
            Cities: [],
            Leaders: [],
            Units: [],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);
    }

    public static WorldCityDefinition SimpleCityDefinition(WorldCityDefinitionId id, WorldResourceSet upgradeCost)
    {
        return new WorldCityDefinition(
            Id: id,
            Level: id.Level,
            VisionRadius: 4,
            Regeneration: 5 * (id.Level + 1),
            UpgradeCost: upgradeCost);
    }

    public static WorldPlayerBuildingDefinition SimpleBuildingDefinition(WorldPlayerBuildingDefinitionId id, WorldPlayerBuildingCategory category)
    {
        return new WorldPlayerBuildingDefinition(
            Id: id,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: category,
            UnitBranch: null,
            Level: 1,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);
    }

    public static WorldSpellDefinition SimpleSpellDefinition(WorldSpellDefinitionId id, WorldAbstractSpellEffect effect)
    {
        return new WorldSpellDefinition(
            Id: id,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            EffectDescriptionTextId: new GlobalTextId("effect"),
            Race: WorldRace.Human,
            Level: 1,
            CastingCost: WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: 0,
            Effect: effect,
            AiType: WorldAiSpellType.All);
    }

    public static ItemDefinition SimpleItemDefinition(ItemDefinitionId id, ItemCategory category)
    {
        return new ItemDefinition(
            Id: id,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            Category: category,
            Value: WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);
    }

    public static WorldCity SimpleCity(
        WorldFortificationId id,
        WorldCityDefinition definition,
        WorldPlayerId ownerId,
        WorldPosition position,
        UnitGrid<WorldUnit> garrison)
    {
        return new WorldCity(
            Id: id,
            NameTextId: new ScenarioTextId("Name"),
            DefinitionId: definition.Id,
            OwnerId: ownerId,
            SubraceId: NeutralSubrace.Id,
            Footprint: FortificationCalculator.CreateFootprint(position, FortificationCalculator.CityFootprint),
            Garrison: garrison,
            Inventory: WorldInventory.Empty,
            AiPriority: 3,
            RiotTurns: 0,
            GrowthTurn: 0);
    }

    public static WorldCapital SimpleCapital(
        WorldFortificationId id,
        WorldPlayerId ownerId,
        WorldPosition position,
        UnitGrid<WorldUnit> garrison)
    {
        return new WorldCapital(
            Id: id,
            NameTextId: new ScenarioTextId("Name"),
            OwnerId: ownerId,
            Footprint: FortificationCalculator.CreateFootprint(position, FortificationCalculator.CapitalFootprint),
            Garrison: garrison,
            Inventory: WorldInventory.Empty,
            AiPriority: 3,
            CapturedById: null);
    }

    public static WorldStack SimpleStack(
        WorldStackId id,
        WorldPlayerId ownerId,
        WorldPosition position,
        UnitGrid<WorldUnit> units,
        int movementPoints)
    {
        return new WorldStack(
            Id: id,
            OwnerId: ownerId,
            Position: position,
            Direction: WorldStackDirection.South,
            Units: units,
            MovementPoints: movementPoints,
            Inventory: WorldInventory.Empty,
            Equipment: LeaderEquipment.Empty,
            SubraceId: NeutralSubrace.Id,
            AiPriority: 3,
            AiIgnore: false,
            Concealed: false,
            Order: WorldStackOrder.Normal.Instance,
            ExpirationTurn: null,
            PendingLeaderUpgrades: 0,
            BattlesWon: 0,
            SourceTemplateId: null);
    }

    public static WorldMap SimpleMap(int width, int height, char terrain = '.')
    {
        List<string> rows = [];

        for (int y = 0; y < height; y++)
        {
            rows.Add(new string(terrain, width));
        }

        return BuildMap(rows);
    }

    public static WorldMap BuildMap(IEnumerable<string> rows)
    {
        string[] rowsArray = [.. rows];
        int width = 0;

        foreach (string row in rowsArray)
        {
            if (row.Length > width)
            {
                width = row.Length;
            }
        }

        List<WorldMapCell> cells = [];

        foreach (string row in rowsArray)
        {
            foreach (char ch in row)
            {
                WorldMapTerrain mapTerrain = ch switch
                {
                    '.' or '^' => WorldMapTerrain.Neutral,
                    '~' => WorldMapTerrain.Water,
                    '+' or '=' => WorldMapTerrain.Neutral,
                    >= '0' and <= '9' => WorldMapTerrain.Neutral,
                    'H' => WorldMapTerrain.Human,
                    'U' => WorldMapTerrain.Undead,
                    'R' => WorldMapTerrain.Heretic,
                    'D' => WorldMapTerrain.Dwarf,
                    'E' => WorldMapTerrain.Elf,
                    _ => throw new InvalidDataException($"Invalid character in map: '{ch}'.")
                };

                WorldMapFeature feature = ch switch
                {
                    '^' => WorldMapFeature.Mountain,
                    '+' => WorldMapFeature.Forest,
                    >= '0' and <= '9' => WorldMapFeature.Forest,
                    '=' => WorldMapFeature.Road,
                    _ => WorldMapFeature.None
                };

                WorldObjectVariant featureVariant;
                if (ch >= '0' && ch <= '9')
                {
                    featureVariant = new WorldObjectVariant(ch - '0');
                }
                else
                {
                    featureVariant = default;
                }

                cells.Add(new WorldMapCell(mapTerrain, feature, featureVariant));
            }
        }

        return new WorldMap(width, rowsArray.Length, [.. cells]);
    }

    public static ImmutableBitmap BuildFogOfWar(IEnumerable<string> rows)
    {
        string[] rowsArray = [.. rows];
        int width = 0;

        foreach (string row in rowsArray)
        {
            if (row.Length > width)
            {
                width = row.Length;
            }
        }

        ImmutableBitmap.Builder fogOfWar = ImmutableBitmap.Create(width, rowsArray.Length).ToBuilder();

        for (int y = 0; y < rowsArray.Length; y++)
        {
            int x = 0;

            foreach (char ch in rowsArray[y])
            {
                switch (ch)
                {
                    case '.':
                        fogOfWar.Set(x, y);
                        break;
                    case '?':
                        break;
                    default:
                        throw new InvalidDataException($"Invalid character in fog of war: '{ch}'.");
                }

                x++;
            }
        }

        return fogOfWar.ToImmutable();
    }

    public static IEnumerable<string> SerializeMap(WorldMap map)
    {
        for (int y = 0; y < map.Height; y++)
        {
            StringBuilder sb = new();

            for (int x = 0; x < map.Width; x++)
            {
                WorldPosition position = new(x, y);

                if (map.IsValidPosition(position))
                {
                    WorldMapCell cell = map.GetCellAt(position);
                    char terrainChar = GetTerrainChar(cell);
                    sb.Append(terrainChar);
                }
                else
                {
                    sb.Append(' ');
                }
            }

            yield return sb.ToString();
        }
    }

    private static char GetTerrainChar(WorldMapCell cell)
    {
        if (cell.Terrain == WorldMapTerrain.Water)
        {
            return '~';
        }

        if (cell.Feature != WorldMapFeature.None)
        {
            return cell.Feature switch
            {
                WorldMapFeature.Mountain => '^',
                WorldMapFeature.Forest => '+',
                WorldMapFeature.Road => '=',
                _ => '?'
            };
        }

        return cell.Terrain switch
        {
            WorldMapTerrain.Human => 'H',
            WorldMapTerrain.Undead => 'U',
            WorldMapTerrain.Heretic => 'R',
            WorldMapTerrain.Dwarf => 'D',
            WorldMapTerrain.Elf => 'E',
            WorldMapTerrain.Neutral => '.',
            _ => '?'
        };
    }

    public static IEnumerable<string> SerializeFogOfWar(ImmutableBitmap fog)
    {
        for (int y = 0; y < fog.Height; y++)
        {
            StringBuilder sb = new();

            for (int x = 0; x < fog.Width; x++)
            {
                sb.Append(fog.Get(x, y) ? '.' : '?');
            }

            yield return sb.ToString();
        }
    }
}
