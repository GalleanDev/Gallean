using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("GItem.json")]
    public record Item(
        [property: JsonPropertyName("ITEM_CAT")] MagicItemType ItemCategory,
        [property: DataKey, JsonPropertyName("ITEM_ID")] Id ItemId,
        [property: JsonPropertyName("NAME_TXT")] string NameText,
        [property: JsonPropertyName("DESC_TXT")] string DescriptionText,
        [property: JsonPropertyName("IMAGE_ID")] string ImageId,
        [property: JsonPropertyName("VALUE")] ResourceSet Value,
        [property: JsonPropertyName("MOD_EQUIP")] Id? ModifierEquipment,
        [property: JsonPropertyName("MOD_POTION")] Id? ModifierPotion,
        [property: JsonPropertyName("HP_POTION")] int? HitpointsPotion,
        [property: JsonPropertyName("SPELL_ID")] Id? SpellId,
        [property: JsonPropertyName("CASTING")] ResourceSet? Casting,
        [property: JsonPropertyName("ATTACK_ID")] Id? AttackId,
        [property: JsonPropertyName("UNIT_ID")] Id? UnitId
    );
}
