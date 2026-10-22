namespace Extractor;

internal sealed record ScenarioFileTables
{
    public IReadOnlyList<Db.ScenarioBag> ScenarioBags { get; init; } = [];
    public IReadOnlyList<Db.ScenarioCrystal> ScenarioCrystals { get; init; } = [];
    public IReadOnlyList<Db.ScenarioEvent> ScenarioEvents { get; init; } = [];
    public IReadOnlyList<Db.ScenarioEventCondition> ScenarioEventConditions { get; init; } = [];
    public IReadOnlyList<Db.ScenarioEventEffect> ScenarioEventEffects { get; init; } = [];
    public IReadOnlyList<Db.ScenarioEventUnfog> ScenarioEventUnfogs { get; init; } = [];
    public IReadOnlyList<Db.ScenarioFog> ScenarioFogs { get; init; } = [];
    public IReadOnlyList<Db.ScenarioFortification> ScenarioFortifications { get; init; } = [];
    public IReadOnlyList<Db.ScenarioInfo> ScenarioInfos { get; init; } = [];
    public IReadOnlyList<Db.ScenarioInventory> ScenarioInventories { get; init; } = [];
    public IReadOnlyList<Db.ScenarioItem> ScenarioItems { get; init; } = [];
    public IReadOnlyList<Db.ScenarioLandmark> ScenarioLandmarks { get; init; } = [];
    public IReadOnlyList<Db.ScenarioLocation> ScenarioLocations { get; init; } = [];
    public IReadOnlyList<Db.ScenarioMage> ScenarioMages { get; init; } = [];
    public IReadOnlyList<Db.ScenarioMapBlock> ScenarioMapBlocks { get; init; } = [];
    public IReadOnlyList<Db.ScenarioMapElement> ScenarioMapElements { get; init; } = [];
    public IReadOnlyList<Db.ScenarioMercenary> ScenarioMercenaries { get; init; } = [];
    public IReadOnlyList<Db.ScenarioMerchant> ScenarioMerchants { get; init; } = [];
    public IReadOnlyList<Db.ScenarioMountain> ScenarioMountains { get; init; } = [];
    public IReadOnlyList<Db.ScenarioObjectMap> ScenarioObjectMaps { get; init; } = [];
    public IReadOnlyList<Db.ScenarioPlayer> ScenarioPlayers { get; init; } = [];
    public IReadOnlyList<Db.ScenarioRelation> ScenarioRelations { get; init; } = [];
    public IReadOnlyList<Db.ScenarioRoad> ScenarioRoads { get; init; } = [];
    public IReadOnlyList<Db.ScenarioRod> ScenarioRods { get; init; } = [];
    public IReadOnlyList<Db.ScenarioRuin> ScenarioRuins { get; init; } = [];
    public IReadOnlyList<Db.ScenarioSite> ScenarioSites { get; init; } = [];
    public IReadOnlyList<Db.ScenarioSpell> ScenarioSpells { get; init; } = [];
    public IReadOnlyList<Db.ScenarioStack> ScenarioStacks { get; init; } = [];
    public IReadOnlyList<Db.ScenarioStackTemplate> ScenarioStackTemplates { get; init; } = [];
    public IReadOnlyList<Db.ScenarioStackTemplateModifier> ScenarioStackTemplateModifiers { get; init; } = [];
    public IReadOnlyList<Db.ScenarioSubrace> ScenarioSubraces { get; init; } = [];
    public IReadOnlyList<Db.ScenarioTalismanCharges> ScenarioTalismanCharges { get; init; } = [];
    public IReadOnlyList<Db.ScenarioUnit> ScenarioUnits { get; init; } = [];
    public IReadOnlyList<Db.ScenarioUnitGroup> ScenarioUnitGroups { get; init; } = [];
    public IReadOnlyList<Db.ScenarioUnitModifier> ScenarioUnitModifiers { get; init; } = [];
    public IReadOnlyList<Db.ScenarioVariable> ScenarioVariables { get; init; } = [];
    public IReadOnlyList<Db.TextScenario> TextScenarios { get; init; } = [];
}
