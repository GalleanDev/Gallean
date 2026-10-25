using System.Collections.Immutable;

namespace Gallean.Engine;

public sealed record WorldRaceDefinition(
    WorldRace Race,
    GlobalTextId NameTextId,
    WorldResourceSet Income,
    WorldMapTerrain NativeTerrain,
    bool IsPlayable,
    int CapitalRegeneration,
    UnitModifierDefinitionId? CapitalProtectionModifierId,
    ImmutableArray<WorldCityDefinitionId> Cities,
    ImmutableArray<UnitDefinitionId> Leaders,
    ImmutableArray<UnitDefinitionId> Units,
    ImmutableArray<string> MaleLeaderNames,
    ImmutableArray<string> FemaleLeaderNames);
