using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("Grace.json", Index = true)]
    public record Race(
        [property: DataKey, JsonPropertyName("RACE_ID")] Id RaceId,
        [property: JsonPropertyName("GUARDIAN")] Id? GuardianId,
        [property: JsonPropertyName("NOBLE")] Id? NobleId,
        [property: JsonPropertyName("LEADER_1")] Id? Leader1Id,
        [property: JsonPropertyName("LEADER_2")] Id? Leader2Id,
        [property: JsonPropertyName("LEADER_3")] Id? Leader3Id,
        [property: JsonPropertyName("LEADER_4")] Id? Leader4Id,
        [property: JsonPropertyName("SOLDIER_1")] Id? Soldier1Id,
        [property: JsonPropertyName("SOLDIER_2")] Id? Soldier2Id,
        [property: JsonPropertyName("SOLDIER_3")] Id? Soldier3Id,
        [property: JsonPropertyName("SOLDIER_4")] Id? Soldier4Id,
        [property: JsonPropertyName("SOLDIER_5")] Id? Soldier5Id,
        [property: JsonPropertyName("SCOUT")] int? Scout,
        [property: JsonPropertyName("REGEN_H")] int? Regeneration,
        [property: JsonPropertyName("INCOME")] ResourceSet Income,
        [property: JsonPropertyName("PROTECTION")] Id? ProtectionId,
        [property: JsonPropertyName("NAME_TXT")] string NameText,
        [property: JsonPropertyName("RACE_TYPE")] RaceType RaceType,
        [property: JsonPropertyName("PLAYABLE")] bool Playable
    );
}
