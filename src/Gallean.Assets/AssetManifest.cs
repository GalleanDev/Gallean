namespace Gallean.Assets;

public sealed record AssetManifest(
    DateTimeOffset CreatedAt,
    string CreatedBy,
    bool Sparse,
    IReadOnlyList<AssetManifestSprite> Sprites,
    IReadOnlyList<AssetManifestAnimation> Animations,
    IReadOnlyList<AssetManifestSound> Sounds);
