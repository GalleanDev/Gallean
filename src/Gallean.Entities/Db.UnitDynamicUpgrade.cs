using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("GDynUpgr.json")]
    public record UnitDynamicUpgrade(
        [property: DataKey, JsonPropertyName("UPGRADE_ID")] Id UpgradeId,
        [property: JsonPropertyName("ENROLL_C")] ResourceSet EnrollCost,
        [property: JsonPropertyName("HIT_POINT")] int HitPoint,
        [property: JsonPropertyName("ARMOR")] int? Armor,
        [property: JsonPropertyName("REGEN")] int? Regen,
        [property: JsonPropertyName("REVIVE_C")] ResourceSet ReviveCost,
        [property: JsonPropertyName("HEAL_C")] ResourceSet HealCost,
        [property: JsonPropertyName("TRAINING_C")] ResourceSet TrainingCost,
        [property: JsonPropertyName("XP_KILLED")] int ExperienceKilled,
        [property: JsonPropertyName("XP_NEXT")] int ExperienceNext,
        [property: JsonPropertyName("MOVE")] int? Move,
        [property: JsonPropertyName("NEGOTIATE")] int Negotiate,
        [property: JsonPropertyName("DAMAGE")] int? Damage,
        [property: JsonPropertyName("HEAL")] int? Heal,
        [property: JsonPropertyName("INITIATIVE")] int? Initiative,
        [property: JsonPropertyName("POWER")] int? Power
    );
}
