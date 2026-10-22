using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("Sspell.json", PerScenario = true)]
    public record ScenarioSpell(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataLookupKey, JsonPropertyName("KSPELLS_ID")] Id KnownSpellsId,
        [property: JsonPropertyName("SPELL_ID")] Id SpellId
    );
}
