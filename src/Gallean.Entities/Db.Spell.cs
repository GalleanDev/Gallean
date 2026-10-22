using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("Gspells.json")]
    public record Spell(
        [property: DataKey, JsonPropertyName("SPELL_ID")] Id SpellId,
        [property: JsonPropertyName("CATEGORY")] SpellType Category,
        [property: JsonPropertyName("LEVEL")] int Level,
        [property: JsonPropertyName("CASTING_C")] ResourceSet CastingCost,
        [property: JsonPropertyName("BUY_C")] ResourceSet BuyCost,
        [property: JsonPropertyName("NAME_TXT")] string NameText,
        [property: JsonPropertyName("DESC_TXT")] string DescriptionText,
        [property: JsonPropertyName("UNIT_ID")] Id? UnitId,
        [property: JsonPropertyName("RESTORE_M")] int? RestoreMovement,
        [property: JsonPropertyName("AREA")] int Area,
        [property: JsonPropertyName("MODIF_ID")] Id? ModifierId,
        [property: JsonPropertyName("MODIF_TXT")] string ModifierText,
        [property: JsonPropertyName("DAMAGE_SRC")] AttackSourceType? DamageSource,
        [property: JsonPropertyName("DAMAGE_QTY")] int? DamageQuantity,
        [property: JsonPropertyName("HEAL_QTY")] int? HealQuantity,
        [property: JsonPropertyName("AI_CAT")] AiSpellType AiCategory,
        [property: JsonPropertyName("GROUND_CAT")] GroundType? GroundCategory,
        [property: JsonPropertyName("CHNGTERCAT")] int? ChangeTerrainCategory,
        [property: JsonPropertyName("QTY_WARDS")] int? QuantityWards,
        [property: JsonPropertyName("WARD1")] Id? Ward1,
        [property: JsonPropertyName("WARD2")] Id? Ward2,
        [property: JsonPropertyName("WARD3")] Id? Ward3,
        [property: JsonPropertyName("WARD4")] Id? Ward4
    );
}
