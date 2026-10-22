using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SEvEffec.json", PerScenario = true)]
    public record ScenarioEventEffect(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataLookupKey, JsonPropertyName("ID")] Id Id,
        [property: JsonPropertyName("NUM")] int Number,
        [property: JsonPropertyName("CATEGORY")] ScenarioEventEffectType Category,
        [property: JsonPropertyName("ID_LOC")] Id? IdLocation,
        [property: JsonPropertyName("ID_STKTEMP")] Id? IdStackTemplate,
        [property: JsonPropertyName("TYPE_SPELL")] Id? TypeSpell,
        [property: JsonPropertyName("SOUND")] string? Sound,
        [property: JsonPropertyName("MUSIC")] string? Music,
        [property: JsonPropertyName("IMAGE")] string? Image,
        [property: JsonPropertyName("IMAGE2")] string? Image2,
        [property: JsonPropertyName("LEFT_SIDE")] bool? LeftSide,
        [property: JsonPropertyName("ID_STACK")] Id? IdStack,
        [property: JsonPropertyName("FIRST_ONLY")] bool? FirstOnly,
        [property: JsonPropertyName("ID_PLAYER1")] Id? IdPlayer1,
        [property: JsonPropertyName("ID_EVENT")] Id? IdEvent,
        [property: JsonPropertyName("TYPE_ITEM")] Id? TypeItem,
        [property: JsonPropertyName("GIVETO")] int? GiveTo,
        [property: JsonPropertyName("ID_PLAYER2")] Id? IdPlayer2,
        [property: JsonPropertyName("DIPLOMACY")] int? Diplomacy,
        [property: JsonPropertyName("ID_LMARK")] Id? IdLandmark,
        [property: JsonPropertyName("TYPE_LMARK")] Id? TypeLandmark,
        [property: JsonPropertyName("ENABLE")] bool? Enable,
        [property: JsonPropertyName("ORDER")] OrderType? Order,
        [property: JsonPropertyName("ORDER_TARG")] Id? OrderTarget,
        [property: JsonPropertyName("TRIG_ONLY")] bool? TriggerOnly,
        [property: JsonPropertyName("WIN_SCEN")] bool? WinScenario,
        [property: JsonPropertyName("PLAY_ANIM")] bool? PlayAnim,
        [property: JsonPropertyName("POPUP_SHOW")] string? PopupShow,
        [property: JsonPropertyName("PERMALLI")] bool? PermanentAlliance,
        [property: JsonPropertyName("LOOKUP")] int? Lookup,
        [property: JsonPropertyName("NUMVALUE")] int? NumValue,
        [property: JsonPropertyName("BOOLVALUE")] bool? BoolValue,
        [property: JsonPropertyName("NUMVALUE2")] int? NumValue2
    );
}
