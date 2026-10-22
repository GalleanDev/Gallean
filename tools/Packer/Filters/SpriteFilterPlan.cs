namespace Packer;

internal sealed record SpriteFilterPlan(IReadOnlyList<ISpriteFilter> Filters, IReadOnlyList<AssetTransform> Transforms);
