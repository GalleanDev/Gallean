using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("Splayer.json", PerScenario = true)]
    public record ScenarioPlayer(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("PLAYER_ID")] Id PlayerId,
        [property: JsonPropertyName("RACE_ID")] Id RaceId,
        [property: JsonPropertyName("LORD_ID")] Id LordId,
        [property: JsonPropertyName("FOG_ID")] Id FogId,
        [property: JsonPropertyName("RESEAR_T")] int ResearchTurn,
        [property: JsonPropertyName("CONSTR_T")] int ConstructTurn,
        [property: JsonPropertyName("BUILDS_ID")] Id BuildsId,
        [property: JsonPropertyName("KNOWN_ID")] Id KnownId,
        [property: JsonPropertyName("BANK")] ResourceSet Bank,
        [property: JsonPropertyName("SPELL_BANK")] ResourceSet SpellBank,
        [property: JsonPropertyName("ATTITUDE")] AiAttitudeType Attitude,
        [property: JsonPropertyName("FACE")] int Face,
        [property: JsonPropertyName("SPY_1")] Id? Spy1Id,
        [property: JsonPropertyName("SPY_2")] Id? Spy2Id,
        [property: JsonPropertyName("SPY_3")] Id? Spy3Id,
        [property: JsonPropertyName("QTY_BREAKS")] int QuantityBreaks,
        [property: JsonPropertyName("IS_HUMAN")] bool IsHuman,
        [property: JsonPropertyName("CAPT_BY")] Id? CapturedById,
        [property: JsonPropertyName("ALWAYSAI")] bool? AlwaysAi,
        [property: JsonPropertyName("EXMAPID1")] Id? ExMapId1,
        [property: JsonPropertyName("EXMAPID2")] Id? ExMapId2,
        [property: JsonPropertyName("EXMAPID3")] Id? ExMapId3,
        [property: JsonPropertyName("EXMAPTURN1")] int ExMapTurn1,
        [property: JsonPropertyName("EXMAPTURN2")] int ExMapTurn2,
        [property: JsonPropertyName("EXMAPTURN3")] int ExMapTurn3
    );
}
