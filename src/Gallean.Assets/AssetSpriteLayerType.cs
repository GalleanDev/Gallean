using System.Text.Json.Serialization;

namespace Gallean.Assets;

[JsonConverter(typeof(JsonStringEnumConverter<AssetSpriteLayerType>))]
public enum AssetSpriteLayerType
{
    Normal,
    Depth,
    Albedo
}
