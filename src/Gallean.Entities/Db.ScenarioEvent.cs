using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SEvent.json", PerScenario = true, Index = true)]
    public record ScenarioEvent(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("ID")] string Id,
        [property: JsonPropertyName("HUMAN")] bool Human,
        [property: JsonPropertyName("DWARF")] bool Dwarf,
        [property: JsonPropertyName("UNDEAD")] bool Undead,
        [property: JsonPropertyName("HERETIC")] bool Heretic,
        [property: JsonPropertyName("NEUTRAL")] bool Neutral,
        [property: JsonPropertyName("ELF")] bool? Elf,
        [property: JsonPropertyName("ENABLED")] bool Enabled,
        [property: JsonPropertyName("OCCUR_ONCE")] bool OccurOnce,
        [property: JsonPropertyName("CHANCE")] int Chance,
        [property: JsonPropertyName("COND_QTY")] int CondQuantity,
        [property: JsonPropertyName("EFFECT_QTY")] int EffectQuantity,
        [property: JsonPropertyName("VERHUMAN")] bool VerifiedHuman,
        [property: JsonPropertyName("VERDWARF")] bool VerifiedDwarf,
        [property: JsonPropertyName("VERUNDEAD")] bool VerifiedUndead,
        [property: JsonPropertyName("VERHERETIC")] bool VerifiedHeretic,
        [property: JsonPropertyName("VERNEUTRAL")] bool VerifiedNeutral,
        [property: JsonPropertyName("VERELF")] bool? VerifiedElf,
        [property: JsonPropertyName("ORDER")] int Order
    );
}
