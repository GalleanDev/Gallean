using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SSite.json", PerScenario = true)]
    public record ScenarioSite(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("SITE_ID")] Id SiteId,
        [property: JsonPropertyName("IMG_ISO")] int ImageIso,
        [property: JsonPropertyName("IMG_INTF")] string? ImageInterface,
        [property: JsonPropertyName("VISITER")] Id? VisiterId,
        [property: JsonPropertyName("POS_X")] int PositionX,
        [property: JsonPropertyName("POS_Y")] int PositionY,
        [property: JsonPropertyName("QTY_VISIT")] string? QuantityVisit,
        [property: JsonPropertyName("QTY_SPELL")] int? QuantitySpell,
        [property: JsonPropertyName("QTY_UNIT")] int? QuantityUnit,
        [property: JsonPropertyName("QTY_ITEM")] int? QuantityItem,
        [property: JsonPropertyName("BUY_ARMOR")] bool? BuyArmor,
        [property: JsonPropertyName("BUY_JEWEL")] bool? BuyJewel,
        [property: JsonPropertyName("BUY_WEAPON")] bool? BuyWeapon,
        [property: JsonPropertyName("BUY_BANNER")] bool? BuyBanner,
        [property: JsonPropertyName("BUY_POTION")] bool? BuyPotion,
        [property: JsonPropertyName("BUY_SCROLL")] bool? BuyScroll,
        [property: JsonPropertyName("BUY_WAND")] bool? BuyWand,
        [property: JsonPropertyName("BUY_VALUE")] bool? BuyValue,
        [property: JsonPropertyName("MISSION")] bool? Mission,
        [property: JsonPropertyName("AIPRIORITY")] int AiPriority
    );
}
