using System.Text.Json.Serialization;

namespace Gallean.Assets;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(AssetManifest))]
internal sealed partial class AssetPackJsonContext : JsonSerializerContext;
