using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

internal sealed class TestGameDefinitionProvider : IGameDefinitionProvider
{
    private readonly ImmutableDictionary<UnitAttackDefinitionId, UnitAttackDefinition> attacks;
    private readonly ImmutableDictionary<WorldCityDefinitionId, WorldCityDefinition> cities;
    private readonly ImmutableDictionary<ItemDefinitionId, ItemDefinition> items;
    private readonly ImmutableDictionary<WorldLandmarkDefinitionId, WorldLandmarkDefinition> landmarks;
    private readonly ImmutableList<LeaderUpgradeDefinition> leaderUpgrades;
    private readonly ImmutableDictionary<LordDefinitionId, LordDefinition> lords;
    private readonly ImmutableDictionary<WorldAiAttitudeType, WorldAiAttitudeDefinition> aiAttitudes;
    private readonly ImmutableDictionary<WorldRace, WorldRaceDefinition> races;
    private readonly WorldSpellResearchCosts researchableSpells;
    private readonly ImmutableDictionary<WorldSpellDefinitionId, WorldSpellDefinition> spells;
    private readonly ImmutableDictionary<WorldPlayerBuildingDefinitionId, WorldPlayerBuildingDefinition> techBuildings;
    private readonly ImmutableDictionary<WorldThiefActionType, WorldThiefActionDefinition> thiefActions;
    private readonly ImmutableDictionary<UnitDefinitionId, UnitDynamicUpgradeDefinition> unitDynamicUpgrades;
    private readonly ImmutableDictionary<UnitModifierDefinitionId, UnitModifierDefinition> unitModifiers;
    private readonly ImmutableDictionary<UnitDefinitionId, UnitDefinition> units;
    private readonly ImmutableList<UnitUpgradeDefinition> unitUpgrades;

    public TestGameDefinitionProvider(
        IEnumerable<UnitDefinition> units,
        IEnumerable<UnitAttackDefinition> attacks,
        IEnumerable<UnitUpgradeDefinition> unitUpgrades,
        IEnumerable<UnitDynamicUpgradeDefinition> unitDynamicUpgrades,
        IEnumerable<UnitModifierDefinition> unitModifiers,
        IEnumerable<LeaderUpgradeDefinition> leaderUpgrades,
        IEnumerable<LordDefinition> lords,
        IEnumerable<ItemDefinition> items,
        IEnumerable<WorldCityDefinition> cities,
        IEnumerable<WorldLandmarkDefinition> landmarks,
        IEnumerable<WorldPlayerBuildingDefinition> techBuildings,
        IEnumerable<WorldRaceDefinition> races,
        IEnumerable<WorldSpellDefinition> spells,
        IEnumerable<WorldSpellResearchCost> researchableSpells,
        IEnumerable<WorldThiefActionDefinition> thiefActions,
        IEnumerable<WorldAiAttitudeDefinition> aiAttitudes)
    {
        this.units = units.ToImmutableDictionary(u => u.Id);
        this.attacks = attacks.ToImmutableDictionary(a => a.Id);
        this.unitUpgrades = unitUpgrades.ToImmutableList();
        this.unitDynamicUpgrades = unitDynamicUpgrades.ToImmutableDictionary(l => l.UnitDefinitionId);
        this.unitModifiers = unitModifiers.ToImmutableDictionary(m => m.Id);
        this.leaderUpgrades = leaderUpgrades.ToImmutableList();
        this.lords = lords.ToImmutableDictionary(l => l.Id);
        this.items = items.ToImmutableDictionary(i => i.Id);
        this.cities = cities.ToImmutableDictionary(c => c.Id);
        this.landmarks = landmarks.ToImmutableDictionary(l => l.Id);
        this.techBuildings = techBuildings.ToImmutableDictionary(b => b.Id);
        ILookup<WorldRace, WorldCityDefinitionId> raceCities = this.cities.Keys.ToLookup(id => id.Race);
        this.races = races.ToImmutableDictionary(r => r.Race, r => r with { Cities = [.. raceCities[r.Race]] });
        this.spells = spells.ToImmutableDictionary(s => s.Id);
        this.researchableSpells = new WorldSpellResearchCosts(researchableSpells);
        this.thiefActions = thiefActions.ToImmutableDictionary(a => a.Type);
        this.aiAttitudes = aiAttitudes.ToImmutableDictionary(a => a.Type);
    }

    public static IGameDefinitionProvider Empty { get; } = new TestGameDefinitionProvider([], [], [], [], [], [], [], [], [], [], [], CreateDefaultRaces(), [], [], [], CreateDefaultAiAttitudes());

    public UnitDefinition GetUnit(UnitDefinitionId id)
    {
        if (units.TryGetValue(id, out UnitDefinition? unit))
        {
            return unit;
        }

        throw new KeyNotFoundException($"Unit definition '{id}' was not found.");
    }

    public UnitDefinition? TryGetUnit(UnitDefinitionId id)
    {
        return units.GetValueOrDefault(id);
    }

    public UnitAttackDefinition GetAttack(UnitAttackDefinitionId id)
    {
        if (attacks.TryGetValue(id, out UnitAttackDefinition? attack))
        {
            return attack;
        }

        throw new KeyNotFoundException($"Attack definition '{id}' was not found.");
    }

    public ImmutableArray<UnitUpgradeDefinition> GetUnitUpgrades(UnitDefinitionId id)
    {
        return [.. unitUpgrades.Where(u => u.FromUnitId == id)];
    }

    public UnitDynamicUpgradeDefinition GetUnitDynamicUpgrade(UnitDefinitionId id)
    {
        if (unitDynamicUpgrades.TryGetValue(id, out UnitDynamicUpgradeDefinition? dynamicUpgrade))
        {
            return dynamicUpgrade;
        }

        throw new KeyNotFoundException($"Unit stat scaling definition '{id}' was not found.");
    }

    public LordDefinition GetLord(LordDefinitionId id)
    {
        if (lords.TryGetValue(id, out LordDefinition? lord))
        {
            return lord;
        }

        throw new KeyNotFoundException($"Lord definition '{id}' was not found.");
    }

    public ImmutableArray<LeaderUpgradeDefinition> GetLeaderUpgrades(LordDefinitionId id)
    {
        return [.. leaderUpgrades.Where(u => u.LordId == id)];
    }

    public UnitModifierDefinition GetUnitModifier(UnitModifierDefinitionId id)
    {
        if (unitModifiers.TryGetValue(id, out UnitModifierDefinition? modifier))
        {
            return modifier;
        }

        throw new KeyNotFoundException($"Unit modifier definition '{id}' was not found.");
    }

    public ItemDefinition GetItem(ItemDefinitionId id)
    {
        if (items.TryGetValue(id, out ItemDefinition? item))
        {
            return item;
        }

        throw new KeyNotFoundException($"Item definition '{id}' was not found.");
    }

    public WorldPlayerBuildingDefinition GetPlayerBuilding(WorldPlayerBuildingDefinitionId id)
    {
        if (techBuildings.TryGetValue(id, out WorldPlayerBuildingDefinition? building))
        {
            return building;
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

    public WorldLandmarkDefinition GetLandmark(WorldLandmarkDefinitionId id)
    {
        if (landmarks.TryGetValue(id, out WorldLandmarkDefinition? definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Landmark definition '{id}' was not found.");
    }

    public WorldAiAttitudeDefinition GetAiAttitude(WorldAiAttitudeType type)
    {
        if (aiAttitudes.TryGetValue(type, out WorldAiAttitudeDefinition? definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"AI attitude definition '{type}' was not found.");
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
        if (spells.TryGetValue(id, out WorldSpellDefinition? spell))
        {
            return spell;
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
        return researchableSpells.GetSpellResearchCosts(race);
    }

    internal static IReadOnlyList<WorldAiAttitudeDefinition> CreateDefaultAiAttitudes()
    {
        return
        [
            new WorldAiAttitudeDefinition(WorldAiAttitudeType.Small, 100, 115, 120, 110, 110, 110, 95, 100, 70, 100, 130, 145),
            new WorldAiAttitudeDefinition(WorldAiAttitudeType.Medium, 100, 115, 120, 110, 110, 110, 95, 100, 70, 100, 130, 145),
            new WorldAiAttitudeDefinition(WorldAiAttitudeType.Large, 100, 115, 120, 110, 110, 110, 95, 100, 70, 100, 130, 145),
            new WorldAiAttitudeDefinition(WorldAiAttitudeType.Humongous, 100, 115, 120, 110, 110, 110, 95, 100, 70, 100, 130, 145),
            new WorldAiAttitudeDefinition(WorldAiAttitudeType.SmallNoExpansion, 100, 115, 120, 0, 0, 0, 95, 100, 70, 100, 130, 145),
            new WorldAiAttitudeDefinition(WorldAiAttitudeType.MediumNoExpansion, 100, 115, 120, 0, 0, 0, 95, 100, 70, 100, 130, 145),
            new WorldAiAttitudeDefinition(WorldAiAttitudeType.MediumNoExpansionNoRuins, 100, 115, 120, 0, 0, 0, 95, 0, 70, 100, 130, 145)
        ];
    }

    internal static IReadOnlyList<WorldRaceDefinition> CreateDefaultRaces()
    {
        return
        [
            new WorldRaceDefinition(WorldRace.Human, new GlobalTextId("Empire"), new WorldResourceSet(100, 0, 0, 25, 0, 0), WorldMapTerrain.Human, true, 35, null, [], [], [], ["Konrad"], ["Zolian"]),
            new WorldRaceDefinition(WorldRace.Undead, new GlobalTextId("Undead Hordes"), new WorldResourceSet(100, 25, 0, 0, 0, 0), WorldMapTerrain.Undead, true, 35, null, [], [], [], ["Nigosh"], ["Orissia"]),
            new WorldRaceDefinition(WorldRace.Heretic, new GlobalTextId("Legions of the Damned"), new WorldResourceSet(100, 0, 0, 0, 25, 0), WorldMapTerrain.Heretic, true, 35, null, [], [], [], ["Ekrezem"], ["Kisera"]),
            new WorldRaceDefinition(WorldRace.Dwarf, new GlobalTextId("Mountain Clans"), new WorldResourceSet(100, 0, 25, 0, 0, 0), WorldMapTerrain.Dwarf, true, 35, null, [], [], [], ["Calderic"], ["Olga"]),
            new WorldRaceDefinition(WorldRace.Neutral, new GlobalTextId("Neutral"), WorldResourceSet.Empty, WorldMapTerrain.Neutral, false, 0, null, [], [], [], ["Neutrals"], ["Neutrals"]),
            new WorldRaceDefinition(WorldRace.Elf, new GlobalTextId("Elven Alliance"), new WorldResourceSet(100, 0, 0, 0, 0, 25), WorldMapTerrain.Elf, true, 35, null, [], [], [], ["Ime'el"], ["Cillen"])
        ];
    }
}
