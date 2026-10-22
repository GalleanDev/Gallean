using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("Sinfo.json", Index = true)]
    public record ScenarioInfo(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("INFO_ID")] Id InfoId,
        [property: JsonPropertyName("CUR_TURN")] int CurrentTurn,
        [property: JsonPropertyName("MAP_SEED")] int MapSeed,
        [property: JsonPropertyName("MAP_SIZE")] int MapSize,
        [property: JsonPropertyName("QTY_CITIES")] int QuantityCities,
        [property: JsonPropertyName("CREATOR")] string Creator,
        [property: JsonPropertyName("SOURCE_M")] bool SourceM,
        [property: JsonPropertyName("DIFFGAME")] DifficultyType DifficultyGame,
        [property: JsonPropertyName("DIFFSCEN")] DifficultyType DifficultyScenario,
        [property: JsonPropertyName("CAMPAIGN")] Id CampaignId,
        [property: JsonPropertyName("MAX_UNIT")] int MaxUnit,
        [property: JsonPropertyName("MAX_LEADER")] int MaxLeader,
        [property: JsonPropertyName("MAX_CITY")] int MaxCity,
        [property: JsonPropertyName("MAX_SPELL")] int MaxSpell,
        [property: JsonPropertyName("PLAYER_1")] int Player1,
        [property: JsonPropertyName("PLAYER_2")] int Player2,
        [property: JsonPropertyName("PLAYER_3")] int Player3,
        [property: JsonPropertyName("PLAYER_4")] int Player4,
        [property: JsonPropertyName("PLAYER_5")] int Player5,
        [property: JsonPropertyName("PLAYER_6")] int Player6,
        [property: JsonPropertyName("PLAYER_7")] int Player7,
        [property: JsonPropertyName("PLAYER_8")] int Player8,
        [property: JsonPropertyName("PLAYER_9")] int Player9,
        [property: JsonPropertyName("PLAYER_10")] int Player10,
        [property: JsonPropertyName("PLAYER_11")] int Player11,
        [property: JsonPropertyName("PLAYER_12")] int Player12,
        [property: JsonPropertyName("PLAYER_13")] int Player13,
        [property: JsonPropertyName("NAME")] string Name,
        [property: JsonPropertyName("DESC")] string? Description,
        [property: JsonPropertyName("BRIEFING")] string Briefing,
        [property: JsonPropertyName("BRIEFLONG1")] string? BriefingLong1,
        [property: JsonPropertyName("BRIEFLONG2")] string? BriefingLong2,
        [property: JsonPropertyName("BRIEFLONG3")] string? BriefingLong3,
        [property: JsonPropertyName("BRIEFLONG4")] string? BriefingLong4,
        [property: JsonPropertyName("BRIEFLONG5")] string? BriefingLong5,
        [property: JsonPropertyName("DEBUNKW")] string DebunkWin,
        [property: JsonPropertyName("DEBUNKW2")] string? DebunkWin2,
        [property: JsonPropertyName("DEBUNKW3")] string? DebunkWin3,
        [property: JsonPropertyName("DEBUNKW4")] string? DebunkWin4,
        [property: JsonPropertyName("DEBUNKW5")] string? DebunkWin5,
        [property: JsonPropertyName("DEBUNKL")] string DebunkLose,
        [property: JsonPropertyName("SUGG_LVL")] int SuggestedLevel,
        [property: JsonPropertyName("O")] bool O
    );
}
