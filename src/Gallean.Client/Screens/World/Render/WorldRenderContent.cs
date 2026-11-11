using Avalonia;
using Avalonia.Media;
using Gallean.Common;
using SkiaSharp;

namespace Gallean.Client;

internal sealed record WorldRenderContent(
    IReadOnlyList<WorldRenderContent.GroundLayer> GroundLayers,
    WorldRenderContent.WaterLayer? Water,
    SpriteAtlas CoastAtlas,
    SpriteAtlas BorderAtlas,
    SpriteAtlas RoadAtlas,
    WorldRenderContent.TerrainExpansionLayer? TerrainExpansion,
    SpriteAtlas ObjectAtlas,
    IReadOnlyList<int> ObjectSpriteDepths,
    IReadOnlyList<WorldRenderContent.DirectSprite> DirectSprites,
    IReadOnlyList<WorldRenderContent.MountainCloudSprite> MountainClouds,
    IReadOnlyList<WorldRenderContent.StackSprite> Stacks,
    WorldRenderContent.FogLayer? Fog,
    WorldRenderContent.ReachLayer? Reach,
    IReadOnlyList<WorldRenderContent.HighlightLayer> Highlights,
    SKPath MapClip,
    SKImage MinimapImage,
    IReadOnlyList<WorldRenderContent.HitTarget> HitTargets,
    WorldReachShaderProgram ReachShaderProgram,
    WorldHighlightShaderProgram HighlightShaderProgram,
    WorldMountainCloudShaderProgram MountainCloudShaderProgram,
    WorldBattleTransitionShaderProgram BattleTransitionShaderProgram)
{
    public sealed record GroundLayer(SKVertices Mesh, SKShader TerrainShader);

    public sealed record TerrainExpansionLayer(
        IReadOnlyList<GroundLayer> GroundLayers,
        SpriteAtlas CoastAtlas,
        SpriteAtlas BorderAtlas,
        SKPath Clip,
        SKVertices Mask,
        Rect Bounds,
        TimeSpan Fade,
        TimeSpan Duration);

    public sealed record WaterLayer(SKVertices Mesh, WaterShaderProgram ShaderProgram, IReadOnlyList<SKShader> Albedo);

    public sealed record FogLayer(SKVertices Mesh, SKVertices? MapEdgeMesh, SKShader Shader);

    public sealed record ReachLayer(SKVertices? InteriorMesh, SKVertices EdgeMesh, SKShader Tiles);

    public sealed record HighlightLayer(IReadOnlyList<DirectSprite> Sprites, Color Color, Rect Bounds);

    public sealed record HitTarget(WorldPosition Position, Rect Bounds, ImmutableBitmap Mask);

    public sealed record StackSprite(WorldRenderState.Stack Stack, Rect Bounds);

    public sealed record MountainCloudSprite(int Depth, Rect Bounds, SKShader Mask, float Phase, float Strength);

    public closed record DirectSprite(Point Anchor, int Depth, Rect Bounds, double Opacity)
    {
        public sealed record Animation(AssetAnimationKey Key, Point Anchor, int Depth, Rect Bounds, double Opacity)
            : DirectSprite(Anchor, Depth, Bounds, Opacity);

        public sealed record Sprite(AssetSpriteKey Key, Point Anchor, int Depth, Rect Bounds, double Opacity)
            : DirectSprite(Anchor, Depth, Bounds, Opacity);
    }
}
