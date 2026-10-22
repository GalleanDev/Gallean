namespace Gallean.Engine.Tests;

internal sealed class TestGameDefinitionProviderBuilder
{
    private readonly Dictionary<UnitAttackDefinitionId, UnitAttackDefinition> attacks = [];
    private readonly Dictionary<WorldCityDefinitionId, WorldCityDefinition> cities = [];
    private readonly Dictionary<ItemDefinitionId, ItemDefinition> items = [];
    private readonly Dictionary<WorldLandmarkDefinitionId, WorldLandmarkDefinition> landmarks = [];
    private readonly List<LeaderUpgradeDefinition> leaderUpgrades = [];
    private readonly Dictionary<LordDefinitionId, LordDefinition> lords = [];
    private readonly Dictionary<WorldRace, WorldRaceDefinition> races = [];
    private readonly List<WorldSpellResearchCost> researchableSpells = [];
    private readonly Dictionary<WorldSpellDefinitionId, WorldSpellDefinition> spells = [];
    private readonly Dictionary<WorldPlayerBuildingDefinitionId, WorldPlayerBuildingDefinition> techBuildings = [];
    private readonly Dictionary<WorldThiefActionType, WorldThiefActionDefinition> thiefActions = [];
    private readonly Dictionary<UnitDefinitionId, UnitDynamicUpgradeDefinition> unitDynamicUpgrades = [];
    private readonly Dictionary<UnitModifierDefinitionId, UnitModifierDefinition> unitModifiers = [];
    private readonly Dictionary<UnitDefinitionId, UnitDefinition> units = [];
    private readonly List<UnitUpgradeDefinition> unitUpgrades = [];

    public TestGameDefinitionProviderBuilder AddUnit(UnitDefinition definition)
    {
        units[definition.Id] = definition;

        attacks[definition.Attack.Id] = definition.Attack;

        if (definition.SecondaryAttack is not null)
        {
            attacks[definition.SecondaryAttack.Id] = definition.SecondaryAttack;
        }

        return this;
    }

    public TestGameDefinitionProviderBuilder AddAttack(UnitAttackDefinition attack)
    {
        attacks[attack.Id] = attack;
        return this;
    }

    public TestGameDefinitionProviderBuilder AddUnitUpgrade(UnitUpgradeDefinition upgrade)
    {
        unitUpgrades.Add(upgrade);
        return this;
    }

    public TestGameDefinitionProviderBuilder AddUnitDynamicUpgrade(UnitDynamicUpgradeDefinition dynamicUpgrade)
    {
        unitDynamicUpgrades[dynamicUpgrade.UnitDefinitionId] = dynamicUpgrade;
        return this;
    }

    public TestGameDefinitionProviderBuilder AddUnitModifier(UnitModifierDefinition modifier)
    {
        unitModifiers[modifier.Id] = modifier;
        return this;
    }

    public TestGameDefinitionProviderBuilder AddLeaderUpgrade(LeaderUpgradeDefinition upgrade)
    {
        leaderUpgrades.Add(upgrade);
        return this;
    }

    public TestGameDefinitionProviderBuilder AddLord(LordDefinition lord)
    {
        lords[lord.Id] = lord;
        return this;
    }

    public TestGameDefinitionProviderBuilder AddTechBuilding(WorldPlayerBuildingDefinition building)
    {
        techBuildings[building.Id] = building;
        return this;
    }

    public TestGameDefinitionProviderBuilder AddItem(ItemDefinition item)
    {
        items[item.Id] = item;
        return this;
    }

    public TestGameDefinitionProviderBuilder AddCity(WorldCityDefinition city)
    {
        cities[city.Id] = city;
        return this;
    }

    public TestGameDefinitionProviderBuilder AddLandmark(WorldLandmarkDefinition landmark)
    {
        landmarks[landmark.Id] = landmark;
        return this;
    }

    public TestGameDefinitionProviderBuilder AddRace(WorldRaceDefinition race)
    {
        races[race.Race] = race;
        return this;
    }

    public TestGameDefinitionProviderBuilder AddSpell(WorldSpellDefinition spell)
    {
        spells[spell.Id] = spell;
        return this;
    }

    public TestGameDefinitionProviderBuilder AddResearchableSpell(WorldSpellResearchCost researchableSpell)
    {
        researchableSpells.Add(researchableSpell);
        return this;
    }

    public TestGameDefinitionProviderBuilder AddThiefAction(WorldThiefActionDefinition thiefAction)
    {
        thiefActions[thiefAction.Type] = thiefAction;
        return this;
    }

    public IGameDefinitionProvider Build()
    {
        return new TestGameDefinitionProvider(
            units.Values,
            attacks.Values,
            unitUpgrades,
            unitDynamicUpgrades.Values,
            unitModifiers.Values,
            leaderUpgrades,
            lords.Values,
            items.Values,
            cities.Values,
            landmarks.Values,
            techBuildings.Values,
            races.Values.Count > 0 ? races.Values : TestGameDefinitionProvider.CreateDefaultRaces(),
            spells.Values,
            researchableSpells,
            thiefActions.Values,
            TestGameDefinitionProvider.CreateDefaultAiAttitudes());
    }
}
