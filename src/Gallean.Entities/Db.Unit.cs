using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("Gunits.json")]
    public record Unit(
        [property: DataKey, JsonPropertyName("UNIT_ID")] Id UnitId,
        [property: JsonPropertyName("UNIT_CAT")] UnitCategoryType UnitCategory,
        [property: JsonPropertyName("LEVEL")] int Level,
        [property: JsonPropertyName("PREV_ID")] Id? PreviousId,
        [property: JsonPropertyName("RACE_ID")] Id RaceId,
        [property: JsonPropertyName("SUBRACE")] SubraceType SubraceType,
        [property: JsonPropertyName("BRANCH")] UnitBranchType Branch,
        [property: JsonPropertyName("SIZE_SMALL")] bool? SizeSmall,
        [property: JsonPropertyName("SEX_M")] bool? IsMale,
        [property: JsonPropertyName("ENROLL_C")] ResourceSet EnrollCost,
        [property: JsonPropertyName("ENROLL_B")] Id? EnrollBuildingId,
        [property: JsonPropertyName("NAME_TXT")] string NameText,
        [property: JsonPropertyName("DESC_TXT")] string DescriptionText,
        [property: JsonPropertyName("ABIL_TXT")] string AbilityText,
        [property: JsonPropertyName("ATTACK_ID")] Id AttackId,
        [property: JsonPropertyName("ATTACK2_ID")] Id? Attack2Id,
        [property: JsonPropertyName("ATCK_TWICE")] bool? AttackTwice,
        [property: JsonPropertyName("HIT_POINT")] int Hitpoints,
        [property: JsonPropertyName("BASE_UNIT")] Id? BaseUnit,
        [property: JsonPropertyName("ARMOR")] int Armor,
        [property: JsonPropertyName("REGEN")] int Regen,
        [property: JsonPropertyName("REVIVE_C")] ResourceSet ReviveCost,
        [property: JsonPropertyName("HEAL_C")] ResourceSet HealCost,
        [property: JsonPropertyName("TRAINING_C")] ResourceSet TrainingCost,
        [property: JsonPropertyName("XP_KILLED")] int ExperienceKilled,
        [property: JsonPropertyName("UPGRADE_B")] Id? UpgradeBuildingId,
        [property: JsonPropertyName("XP_NEXT")] int ExperienceNext,
        [property: JsonPropertyName("MOVE")] int? Move,
        [property: JsonPropertyName("SCOUT")] int? Scout,
        [property: JsonPropertyName("LIFE_TIME")] int? Lifetime,
        [property: JsonPropertyName("LEADERSHIP")] int? Leadership,
        [property: JsonPropertyName("NEGOTIATE")] int? Negotiate,
        [property: JsonPropertyName("LEADER_CAT")] LeaderType? LeaderCategory,
        [property: JsonPropertyName("DYN_UPG1")] Id DynamicUpgrade1Id,
        [property: JsonPropertyName("DYN_UPG_LV")] int DynamicUpgradeLevel,
        [property: JsonPropertyName("DYN_UPG2")] Id DynamicUpgrade2Id,
        [property: JsonPropertyName("WATER_ONLY")] bool? WaterOnly,
        [property: JsonPropertyName("DEATH_ANIM")] DeathAnimationType DeathAnimation
    );
}
