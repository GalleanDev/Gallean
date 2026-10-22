using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("Tscen.json", PerScenario = true)]
    public record TextScenario(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataLookupKey, JsonPropertyName("ID_OBJECT")] Id IdObject,
        [property: JsonPropertyName("ID_STRING")] string IdString,
        [property: JsonPropertyName("TEXT")] string? Text,
        [property: JsonPropertyName("SEQ_NUM")] int SeqNum
    )
    {
        public const string PopupTextId = "POPUP_TXT";
        public const string NameTextId = "NAME_TXT";
        public const string ObjectiveTextId = "OBJECT_TXT";
        public const string DescriptionTextId = "DESC_TXT";
        public const string RuinTitleTextId = "TITLE";
        public const string RuinDescriptionTextId = "DESC";
        public const string SiteTitleTextId = "TXT_TITLE";
        public const string SiteDescriptionTextId = "TXT_DESC";
    }
}
