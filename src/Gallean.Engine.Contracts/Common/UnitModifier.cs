using System.Text.Json.Serialization;

namespace Gallean.Engine.Contracts;

public sealed record UnitModifier(UnitModifierDefinitionId DefinitionId, int? RemainingTurns)
{
    [JsonRequired]
    public UnitModifierSource Source { get; init; } = UnitModifierSource.Unit;

    public static UnitModifier Permanent(UnitModifierDefinitionId definitionId)
    {
        return new UnitModifier(definitionId, RemainingTurns: null);
    }

    public static UnitModifier FromEquipment(UnitModifierDefinitionId definitionId)
    {
        return new UnitModifier(definitionId, RemainingTurns: null) { Source = UnitModifierSource.Equipment };
    }

    public static UnitModifier FromScenario(UnitModifierDefinitionId definitionId)
    {
        return new UnitModifier(definitionId, RemainingTurns: null) { Source = UnitModifierSource.Scenario };
    }
}
