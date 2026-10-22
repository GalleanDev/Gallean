using Avalonia;
using Avalonia.Skia;
using Gallean.Common;
using SkiaSharp;
using static Gallean.Client.WorldRenderContent;
using static Gallean.Client.WorldRenderLayout;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderContentProvider
{
    private sealed record MinimapContent(SKImage TerrainImage, ObjectLayer Objects, MinimapState Minimap);

    private sealed record ObjectLayer(SpriteAtlas Atlas, int[] SpriteDepths, IReadOnlyList<DirectSprite> DirectSprites);

    private sealed record FogContent(FogLayer? Layer);

    private sealed record WaterContent(WaterLayer? Layer);

    private sealed record StackLayer(IReadOnlyList<StackSprite> Stacks, IReadOnlyList<DirectSprite> DirectSprites);

    private sealed record DirectSpriteContent(IReadOnlyList<DirectSprite> ObjectSprites, IReadOnlyList<DirectSprite> ShieldSprites);

    private abstract record ObjectSprite
    {
        public sealed record Packed(SpriteAtlasSource.AbstractSprite Sprite) : ObjectSprite;

        public sealed record Direct(DirectSprite Sprite) : ObjectSprite;
    }

    private readonly record struct EdgeOverlayPixel(int Index, int[] Neighbors);

    private readonly record struct LandOverlayKey(AssetSpriteKey Mask, WorldMapTerrain Terrain, SKPointI Phase);

    private readonly record struct TerrainTexture(SKImage Image, int Scale);

    private const double MinimapNativeHalfWidth = 3;
    private const double MinimapTileOverlap = 1;
    private const float MinimapBlurStrength = 0.6f;
    private const double MinimapCullMargin = 512;
    private const int TerrainTileBleed = 1;
    private const int MaxPackedObjectPixels = 512 * 512;

    private readonly Part<MapTerrain, SpriteAtlas> borderAtlasPart = new();
    private readonly TexturePart borderAtlasTexturePart = new();
    private readonly Part<MapTerrain, SpriteAtlas> coastAtlasPart = new();
    private readonly TexturePart coastAtlasTexturePart = new();
    private readonly Part<DirectSpriteContent, DirectSpriteContent, IReadOnlyList<DirectSprite>> directSpritesPart = new(static content => content);
    private readonly Part<IReadOnlyList<FogTile>, FogContent> fogPart = new();
    private readonly Part<MapTerrain, IReadOnlyList<GroundLayer>> groundLayersPart = new();
    private readonly Part<IReadOnlyList<MapObject>, IReadOnlyList<HitTarget>> hitTargetsPart = new();
    private readonly OverlayPart<IReadOnlyList<MapTileBorder>, LandOverlayKey> landOverlayPart = new();
    private readonly Part<MapTerrain, SKPath> mapClipPart = new();
    private readonly Part<MinimapContent, MinimapContent, SKImage> minimapImagePart = new(static content => content);
    private readonly Part<MapTerrain, SKImage> minimapTerrainPart = new();
    private readonly Part<IReadOnlyList<MapObject>, ObjectLayer> objectAtlasPart = new();
    private readonly TexturePart objectAtlasTexturePart = new();
    private readonly Part<WorldRenderState, HighlightOverlay, IReadOnlyList<HighlightLayer>> highlightsPart = new(static state => state.Highlights);
    private readonly Part<WorldRenderState, ReachOverlay?, ReachContent> reachPart = new(static state => state.Reach);
    private readonly Part<MapTerrain, SpriteAtlas> roadAtlasPart = new();
    private readonly TexturePart roadAtlasTexturePart = new();
    private readonly Part<IReadOnlyList<MapObject>, StackLayer> stackLayerPart = new();
    private readonly Part<MapTerrain, Terrains> terrainsPart = new();
    private readonly Part<Terrains, Terrains, IReadOnlyDictionary<WorldMapTerrain, TerrainTexture>> terrainTexturesPart = new(static terrains => terrains);
    private readonly Part<WaterTiles, WaterTiles, WaterContent> waterPart = new(static tiles => tiles);
    private readonly Part<MapTerrain, WaterTiles> waterTilesPart = new();
    private readonly WorldReachShaderProgram reachShaderProgram = Reach.BuildProgram();
    private readonly WorldHighlightShaderProgram highlightShaderProgram = new();
    private readonly WorldBattleTransitionShaderProgram battleTransitionShaderProgram = new();

    public WorldRenderContent Get(WorldRenderState state, IAssetProvider assets)
    {
        Terrains terrains = terrainsPart.Resolve(state.Terrain, Terrains.Build);
        IReadOnlyDictionary<WorldMapTerrain, TerrainTexture> terrainTextures = terrainTexturesPart.Resolve(terrains, source => BuildTerrainTextures(source, assets));
        IReadOnlyList<GroundLayer> groundLayers = groundLayersPart.Resolve(state.Terrain, terrain => BuildGroundLayers(terrain, terrainTextures));
        WaterTiles waterTiles = waterTilesPart.Resolve(state.Terrain, WaterTiles.Build);
        WaterContent water = waterPart.Resolve(waterTiles, tiles => new WaterContent(Water.Build(tiles, assets)));
        SpriteAtlas coastAtlas = coastAtlasPart.Resolve(state.Terrain, terrain => BuildCoastAtlas(terrain, assets));
        SpriteAtlas borderAtlas = borderAtlasPart.Resolve(state.Terrain, terrain => BuildBorderAtlas(terrain, terrainTextures, assets));
        SpriteAtlas roadAtlas = roadAtlasPart.Resolve(state.Terrain, terrain => BuildRoadAtlas(terrain, assets));
        ObjectLayer objectLayer = objectAtlasPart.Resolve(state.MapObjects, objects => BuildObjectAtlas(objects, assets));
        StackLayer stackLayer = stackLayerPart.Resolve(state.Stacks, objects => BuildStackLayer(objects, assets));
        IReadOnlyList<DirectSprite> directSprites = directSpritesPart.Resolve(new DirectSpriteContent(objectLayer.DirectSprites, stackLayer.DirectSprites), MergeDirectSprites);
        FogContent fog = fogPart.Resolve(state.FogTiles, fogTiles => Fog.Build(fogTiles, state.Terrain.Map));
        ReachContent reach = reachPart.Resolve(state, source => Reach.Build(source.Reach));
        IReadOnlyList<HighlightLayer> highlights = highlightsPart.Resolve(state, source => Highlights.Build(source.Highlights, assets));
        SKPath mapClip = mapClipPart.Resolve(state.Terrain, terrain => BuildMapClip(terrain.Map));
        SKImage minimapTerrainImage = minimapTerrainPart.Resolve(state.Terrain, terrain => BuildMinimapTerrainImage(terrain, groundLayers, water.Layer, coastAtlas, borderAtlas, roadAtlas));
        SKImage minimapImage = minimapImagePart.Resolve(new MinimapContent(minimapTerrainImage, objectLayer, state.Minimap), content => BuildMinimapImage(content, assets));
        IReadOnlyList<HitTarget> hitTargets = hitTargetsPart.Resolve(state.Stacks, objects => BuildHitTargets(objects, assets));

        return new WorldRenderContent(
            groundLayers,
            water.Layer,
            coastAtlas,
            borderAtlas,
            roadAtlas,
            objectLayer.Atlas,
            objectLayer.SpriteDepths,
            directSprites,
            stackLayer.Stacks,
            fog.Layer,
            reach.Layer,
            highlights,
            mapClip,
            minimapImage,
            hitTargets,
            reachShaderProgram,
            highlightShaderProgram,
            battleTransitionShaderProgram);
    }

    private static Dictionary<WorldMapTerrain, TerrainTexture> BuildTerrainTextures(Terrains terrains, IAssetProvider assets)
    {
        using CleanupList leases = new();

        Dictionary<WorldMapTerrain, TerrainTexture> textures = [];

        foreach (WorldMapTerrain terrain in terrains.Values)
        {
            textures[terrain] = BuildTerrainTexture(terrain, assets, leases);
        }

        return textures;
    }

    private static List<GroundLayer> BuildGroundLayers(MapTerrain terrain, IReadOnlyDictionary<WorldMapTerrain, TerrainTexture> terrainTextures)
    {
        Dictionary<WorldMapTerrain, List<WorldPosition>> regions = [];

        foreach (MapTile tile in terrain.MapTiles)
        {
            if (tile.Surface is MapTileSurface.Ground ground)
            {
                if (!regions.TryGetValue(ground.Terrain, out List<WorldPosition>? tiles))
                {
                    tiles = [];
                    regions[ground.Terrain] = tiles;
                }

                tiles.Add(tile.Position);
            }
        }

        List<GroundLayer> layers = [];

        foreach (WorldMapTerrain groundTerrain in terrain.GroundTerrains)
        {
            List<WorldPosition> tiles = regions[groundTerrain];
            TerrainTexture texture = terrainTextures[groundTerrain];
            SKMatrix transform = SKMatrix.CreateScale(1.0f / texture.Scale, 1.0f / texture.Scale);

            SKShader terrainShader = texture.Image.ToShader(
                SKShaderTileMode.Repeat,
                SKShaderTileMode.Repeat,
                RenderSamplingOptions.Linear,
                transform);

            layers.Add(new GroundLayer(BuildTileMesh(tiles, TerrainTileBleed), terrainShader));
        }

        return layers;
    }

    private static SKVertices BuildTileMesh(IReadOnlyList<WorldPosition> tiles, int bleed)
    {
        Dictionary<(int X, int Y), ushort> corners = [];
        List<SKPoint> positions = [];
        List<ushort> indexes = [];

        foreach (WorldPosition tile in tiles)
        {
            ushort top = Corner(tile.X, tile.Y);
            ushort right = Corner(tile.X + 1, tile.Y);
            ushort bottom = Corner(tile.X + 1, tile.Y + 1);
            ushort left = Corner(tile.X, tile.Y + 1);

            positions[top] += new SKPoint(0, -bleed / 2.0f);
            positions[right] += new SKPoint(bleed, 0);
            positions[bottom] += new SKPoint(0, bleed / 2.0f);
            positions[left] += new SKPoint(-bleed, 0);

            indexes.AddRange([top, right, bottom, top, bottom, left]);
        }

        return SKVertices.CreateCopy(SKVertexMode.Triangles, [.. positions], null, null, [.. indexes]);

        ushort Corner(int x, int y)
        {
            if (!corners.TryGetValue((x, y), out ushort index))
            {
                Point point = WorldTransform.ToWorldPoint(x - 0.5, y - 0.5);
                index = (ushort)positions.Count;
                positions.Add(point.ToSKPoint());
                corners[(x, y)] = index;
            }

            return index;
        }
    }

    private static TerrainTexture BuildTerrainTexture(WorldMapTerrain terrain, IAssetProvider assets, CleanupList leases)
    {
        Sprite ground = leases.Add(assets.GetRequiredSprite(new AssetSpriteKey.Ground(terrain))).Value;
        int scale = ground.Metadata.PixelScale;

        SKImageInfo info = new(ground.Metadata.PixelWidth, ground.Metadata.PixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        SKCanvas canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        canvas.DrawImage(ground.Image, 0, 0, RenderSamplingOptions.Nearest);

        SKImage image = surface.CreateRasterImage();

        return new TerrainTexture(image, scale);
    }

    private static SKPointI GetTerrainPhase(WorldPosition position, TerrainTexture texture)
    {
        Point center = WorldTransform.ToWorldPoint(position);
        int left = (int)center.X - WorldTransform.TileHalfWidth;
        int top = (int)center.Y - WorldTransform.TileHalfHeight;

        return new SKPointI(left.Mod(texture.Image.Width / texture.Scale), top.Mod(texture.Image.Height / texture.Scale));
    }

    private SpriteAtlas BuildBorderAtlas(MapTerrain terrain, IReadOnlyDictionary<WorldMapTerrain, TerrainTexture> terrainTextures, IAssetProvider assets)
    {
        using CleanupList leases = new();

        List<SpriteAtlasSource.AbstractSprite> sprites = [.. GetBorderSprites(terrain.Borders, terrainTextures, assets, leases)];
        SpriteAtlasSource.Plan plan = SpriteAtlasSource.CreatePlan(sprites);
        AtlasTexture texture = borderAtlasTexturePart.Resolve(plan.Regions, AtlasTexture.Build);

        return SpriteAtlasSource.FromPlan(plan, texture).CreateAtlas();
    }

    private IEnumerable<SpriteAtlasSource.AbstractSprite> GetBorderSprites(
        IReadOnlyList<MapTileBorder> borders,
        IReadOnlyDictionary<WorldMapTerrain, TerrainTexture> terrainTextures,
        IAssetProvider assets,
        CleanupList leases)
    {
        Dictionary<AssetSpriteKey, SKImage> maskImages = [];
        Dictionary<int, EdgeOverlayPixel[]> edgePixels = [];

        foreach (MapTileBorder border in borders)
        {
            TerrainTexture terrainTexture = terrainTextures[border.GroundTerrain];
            SKPointI phase = GetTerrainPhase(border.Position, terrainTexture);

            SKImage overlayImage = landOverlayPart.Resolve(
                borders,
                new LandOverlayKey(border.MaskKey, border.GroundTerrain, phase),
                () => RenderLandOverlay(terrainTexture, phase, GetMaskImage(border.MaskKey), GetEdgePixels(terrainTexture.Scale)));

            Point center = WorldTransform.ToWorldPoint(border.Position);

            yield return new SpriteAtlasSource.StaticSprite(
                new AtlasTexture.RegionKey(overlayImage, overlayImage.Width, overlayImage.Height),
                center.X - WorldTransform.TileHalfWidth,
                center.Y - WorldTransform.TileHalfHeight,
                WorldTransform.TileWidth);
        }

        yield break;

        SKImage GetMaskImage(AssetSpriteKey key)
        {
            if (!maskImages.TryGetValue(key, out SKImage? image))
            {
                image = leases.Add(assets.GetRequiredSprite(key)).Value.Image;
                maskImages[key] = image;
            }

            return image;
        }

        EdgeOverlayPixel[] GetEdgePixels(int scale)
        {
            if (!edgePixels.TryGetValue(scale, out EdgeOverlayPixel[]? pixels))
            {
                pixels = CreateEdgeOverlayPixels(WorldTransform.TileWidth * scale, WorldTransform.TileHeight * scale);
                edgePixels[scale] = pixels;
            }

            return pixels;
        }
    }

    private static SKImage RenderLandOverlay(TerrainTexture terrainTexture, SKPointI phase, SKImage mask, EdgeOverlayPixel[] edgePixels)
    {
        int scale = terrainTexture.Scale;
        SKImageInfo info = new(WorldTransform.TileWidth * scale, WorldTransform.TileHeight * scale, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        SKCanvas canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        canvas.Scale(scale);

        SKMatrix terrainTransform = SKMatrix.CreateScaleTranslation(1.0f / scale, 1.0f / scale, -phase.X, -phase.Y);
        SKMatrix maskTransform = SKMatrix.CreateScale((float)WorldTransform.TileWidth / mask.Width, (float)WorldTransform.TileHeight / mask.Height);

        using SKShader terrainShader = terrainTexture.Image.ToShader(SKShaderTileMode.Repeat, SKShaderTileMode.Repeat, RenderSamplingOptions.Nearest, terrainTransform);
        using SKShader maskShader = mask.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, RenderSamplingOptions.Linear, maskTransform);
        using SKPaint terrainPaint = new() { IsAntialias = false, Shader = terrainShader };
        using SKPaint maskPaint = new() { BlendMode = SKBlendMode.DstIn, Shader = maskShader };
        using SKVertices tile = MakeTerrainTileMesh();

        canvas.DrawVertices(tile, SKBlendMode.Modulate, terrainPaint);
        canvas.DrawRect(new SKRect(0, 0, WorldTransform.TileWidth, WorldTransform.TileHeight), maskPaint);

        using SKPixmap pixels = surface.PeekPixels();

        return AdjustEdgeOverlay(info, pixels.GetPixelSpan(), edgePixels);
    }

    private static SKImage AdjustEdgeOverlay(SKImageInfo info, ReadOnlySpan<byte> source, EdgeOverlayPixel[] edgePixels)
    {
        const int alphaChannel = 3;

        byte[] target = [.. source];

        foreach (EdgeOverlayPixel pixel in edgePixels)
        {
            int bestIndex = pixel.Index;

            foreach (int neighbor in pixel.Neighbors)
            {
                if (source[neighbor + alphaChannel] > source[bestIndex + alphaChannel])
                {
                    bestIndex = neighbor;
                }
            }

            source.Slice(bestIndex, 4).CopyTo(target.AsSpan(pixel.Index, 4));
        }

        return SKImage.FromPixelCopy(info, target);
    }

    private static EdgeOverlayPixel[] CreateEdgeOverlayPixels(int width, int height)
    {
        List<EdgeOverlayPixel> pixels = [];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!IsInsideTile(x, y, width, height))
                {
                    List<int> neighbors = [];
                    bool touchesTile = false;

                    for (int neighborY = Math.Max(0, y - 1); neighborY <= Math.Min(height - 1, y + 1); neighborY++)
                    {
                        for (int neighborX = Math.Max(0, x - 1); neighborX <= Math.Min(width - 1, x + 1); neighborX++)
                        {
                            neighbors.Add(((neighborY * width) + neighborX) * 4);
                            touchesTile |= IsInsideTile(neighborX, neighborY, width, height);
                        }
                    }

                    if (touchesTile)
                    {
                        pixels.Add(new EdgeOverlayPixel(((y * width) + x) * 4, [.. neighbors]));
                    }
                }
            }
        }

        return [.. pixels];
    }

    private static bool IsInsideTile(int x, int y, int width, int height)
    {
        double halfWidth = width / 2.0;
        double halfHeight = height / 2.0;

        return (Math.Abs(x + 0.5 - halfWidth) / halfWidth) + (Math.Abs(y + 0.5 - halfHeight) / halfHeight) <= 1;
    }

    private static SKVertices MakeTerrainTileMesh()
    {
        SKPoint top = new(WorldTransform.TileHalfWidth, 0);
        SKPoint right = new(WorldTransform.TileWidth, WorldTransform.TileHalfHeight);
        SKPoint bottom = new(WorldTransform.TileHalfWidth, WorldTransform.TileHeight);
        SKPoint left = new(0, WorldTransform.TileHalfHeight);

        SKPoint[] positions = [top, right, bottom, top, bottom, left];

        return SKVertices.CreateCopy(SKVertexMode.Triangles, positions, null, null);
    }

    private SpriteAtlas BuildCoastAtlas(MapTerrain terrain, IAssetProvider assets)
    {
        using CleanupList leases = new();

        List<SpriteAtlasSource.AbstractSprite> sprites = [.. GetCoastSprites(terrain.Borders, assets, leases)];
        SpriteAtlasSource.Plan plan = SpriteAtlasSource.CreatePlan(sprites);
        AtlasTexture texture = coastAtlasTexturePart.Resolve(plan.Regions, AtlasTexture.Build);

        return SpriteAtlasSource.FromPlan(plan, texture).CreateAtlas();
    }

    private static IEnumerable<SpriteAtlasSource.AbstractSprite> GetCoastSprites(IReadOnlyList<MapTileBorder> borders, IAssetProvider assets, CleanupList leases)
    {
        Dictionary<AssetSpriteKey, Sprite> coastSprites = [];

        foreach (MapTileBorder border in borders)
        {
            if (border is MapTileBorder.Coast coast)
            {
                if (!coastSprites.TryGetValue(coast.CoastKey, out Sprite? coastSprite))
                {
                    coastSprite = leases.Add(assets.GetRequiredSprite(coast.CoastKey)).Value;
                    coastSprites[coast.CoastKey] = coastSprite;
                }

                Point center = WorldTransform.ToWorldPoint(coast.Position);

                yield return new SpriteAtlasSource.StaticSprite(
                    new AtlasTexture.RegionKey(coastSprite.Image, coastSprite.Image.Width, coastSprite.Image.Height),
                    center.X - coastSprite.Metadata.Width / 2.0,
                    center.Y - coastSprite.Metadata.Height / 2.0,
                    coastSprite.Metadata.Width);
            }
        }
    }

    private SpriteAtlas BuildRoadAtlas(MapTerrain terrain, IAssetProvider assets)
    {
        using CleanupList leases = new();

        List<SpriteAtlasSource.AbstractSprite> sprites = [.. GetRoadSprites(terrain.MapTiles, assets, leases)];
        SpriteAtlasSource.Plan plan = SpriteAtlasSource.CreatePlan(sprites);
        AtlasTexture texture = roadAtlasTexturePart.Resolve(plan.Regions, AtlasTexture.Build);

        return SpriteAtlasSource.FromPlan(plan, texture).CreateAtlas();
    }

    private static IEnumerable<SpriteAtlasSource.AbstractSprite> GetRoadSprites(IReadOnlyList<MapTile> tiles, IAssetProvider assets, CleanupList leases)
    {
        Dictionary<AssetSpriteKey, Sprite> roadSprites = [];

        foreach (MapTile tile in tiles)
        {
            if (tile.RoadKey is not null)
            {
                if (!roadSprites.TryGetValue(tile.RoadKey, out Sprite? roadSprite))
                {
                    roadSprite = leases.Add(assets.GetRequiredSprite(tile.RoadKey)).Value;
                    roadSprites[tile.RoadKey] = roadSprite;
                }

                Point center = WorldTransform.ToWorldPoint(tile.Position);

                yield return new SpriteAtlasSource.StaticSprite(
                    new AtlasTexture.RegionKey(roadSprite.Image, roadSprite.Image.Width, roadSprite.Image.Height),
                    center.X - roadSprite.Metadata.Width / 2.0,
                    center.Y - roadSprite.Metadata.Height / 2.0,
                    roadSprite.Metadata.Width);
            }
        }
    }

    private static SKPath BuildMapClip(WorldMap map)
    {
        Point north = WorldTransform.ToWorldPoint(0, 0);
        Point east = WorldTransform.ToWorldPoint(map.Width - 1, 0);
        Point south = WorldTransform.ToWorldPoint(map.Width - 1, map.Height - 1);
        Point west = WorldTransform.ToWorldPoint(0, map.Height - 1);

        SKPathBuilder builder = new();
        builder.MoveTo((float)north.X, (float)(north.Y - WorldTransform.TileHalfHeight));
        builder.LineTo((float)(east.X + WorldTransform.TileHalfWidth), (float)east.Y);
        builder.LineTo((float)south.X, (float)(south.Y + WorldTransform.TileHalfHeight));
        builder.LineTo((float)(west.X - WorldTransform.TileHalfWidth), (float)west.Y);
        builder.Close();

        return builder.Detach();
    }

    private ObjectLayer BuildObjectAtlas(IReadOnlyList<MapObject> objects, IAssetProvider assets)
    {
        using CleanupList leases = new();

        List<SpriteAtlasSource.AbstractSprite> sprites = [];
        List<int> depths = [];
        List<DirectSprite> directSprites = [];

        foreach (MapObject item in objects)
        {
            foreach (ObjectSprite sprite in GetObjectSprites(item, assets, leases))
            {
                switch (sprite)
                {
                    case ObjectSprite.Packed packed:
                        sprites.Add(packed.Sprite);
                        depths.Add(item.Depth);
                        break;

                    case ObjectSprite.Direct direct:
                        directSprites.Add(direct.Sprite);
                        break;
                }
            }
        }

        SpriteAtlasSource.Plan plan = SpriteAtlasSource.CreatePlan(sprites);
        AtlasTexture texture = objectAtlasTexturePart.Resolve(plan.Regions, AtlasTexture.Build);

        return new ObjectLayer(SpriteAtlasSource.FromPlan(plan, texture).CreateAtlas(), [.. depths], directSprites);
    }

    private static IEnumerable<ObjectSprite> GetObjectSprites(MapObject item, IAssetProvider assets, CleanupList leases)
    {
        Point center = WorldTransform.ToWorldPoint(item.Position);

        switch (item)
        {
            case MapTileFeature feature:
                yield return MakeStatic(feature.AssetKey, leases.Add(assets.GetRequiredSprite(feature.AssetKey)).Value, center);
                break;

            case ResourceNode resource:
                Animation resourceAnimation = leases.Add(assets.GetRequiredAnimation(resource.AssetKey)).Value;
                yield return MakeAnimated(resource.AssetKey, resourceAnimation, Tile.GetAnchor(resource.Position, resourceAnimation.Metadata));
                break;

            case Bag bag:
                yield return MakeStatic(bag.AssetKey, leases.Add(assets.GetRequiredSprite(bag.AssetKey)).Value, center);
                break;

            case Rod rod:
                yield return MakeStatic(rod.AssetKey, leases.Add(assets.GetRequiredSprite(rod.AssetKey)).Value, center);
                break;

            case Tomb tomb:
                yield return MakeStatic(tomb.AssetKey, leases.Add(assets.GetRequiredSprite(tomb.AssetKey)).Value, center);
                break;

            case Fortification fortification:
                Animation fortificationAnimation = leases.Add(assets.GetRequiredAnimation(fortification.AssetKey)).Value;
                yield return MakeAnimated(fortification.AssetKey, fortificationAnimation, Footprint.GetAnchor(fortification.Footprint, fortificationAnimation.Metadata));
                break;

            case Site site:
                Animation siteAnimation = leases.Add(assets.GetRequiredAnimation(site.AssetKey)).Value;
                yield return MakeAnimated(site.AssetKey, siteAnimation, Footprint.GetAnchor(site.Footprint, siteAnimation.Metadata));
                break;

            case Landmark landmark:
                Animation landmarkAnimation = leases.Add(assets.GetRequiredAnimation(landmark.AssetKey)).Value;
                yield return MakeAnimated(landmark.AssetKey, landmarkAnimation, Footprint.GetAnchor(landmark.Footprint, landmarkAnimation.Metadata));
                break;

            case ObjectShadow objectShadow:
                Animation objectShadowAnimation = leases.Add(assets.GetRequiredAnimation(objectShadow.AssetKey)).Value;
                yield return MakeAnimated(objectShadow.AssetKey, objectShadowAnimation, Footprint.GetAnchor(objectShadow.Footprint, objectShadowAnimation.Metadata));
                break;

            case Mountain mountain:
                Sprite mountainSprite = leases.Add(assets.GetRequiredSprite(mountain.AssetKey)).Value;
                yield return MakeStatic(mountain.AssetKey, mountainSprite, Footprint.GetAnchor(mountain.Footprint));
                break;

            case LocationBanner locationBanner:
                Animation locationBannerAnimation = leases.Add(assets.GetRequiredAnimation(locationBanner.AssetKey)).Value;
                yield return MakeAnimated(locationBanner.AssetKey, locationBannerAnimation, Footprint.GetAnchor(locationBanner.Footprint, locationBannerAnimation.Metadata));
                break;

            case RuinBanner ruinBanner:
                Sprite ruinBannerSprite = leases.Add(assets.GetRequiredSprite(ruinBanner.AssetKey)).Value;
                yield return MakeStatic(ruinBanner.AssetKey, ruinBannerSprite, Footprint.GetAnchor(ruinBanner.Footprint));
                break;
        }

        yield break;

        ObjectSprite MakeStatic(AssetSpriteKey key, Sprite sprite, Point anchor)
        {
            double left = anchor.X + sprite.Metadata.OffsetX;
            double top = anchor.Y + sprite.Metadata.OffsetY;

            if (IsPackableSprite(sprite))
            {
                return new ObjectSprite.Packed(new SpriteAtlasSource.StaticSprite(MakeRegionKey(sprite), left, top, sprite.Metadata.Width));
            }
            else
            {
                double height = sprite.Image.Height * (sprite.Metadata.Width / (double)sprite.Image.Width);
                Rect bounds = new(left, top, sprite.Metadata.Width, height);

                return new ObjectSprite.Direct(new DirectSprite.Sprite(key, anchor, item.Depth, bounds, Opacity: 1.0));
            }
        }

        ObjectSprite MakeAnimated(AssetAnimationKey key, Animation animation, Point anchor)
        {
            if (IsPackableAnimation(animation))
            {
                List<AtlasTexture.RegionKey> regions = [];
                List<Sprite> frames = [];

                for (int frameIndex = 0; frameIndex < animation.Metadata.FrameCount; frameIndex++)
                {
                    Sprite frame = animation[frameIndex];
                    regions.Add(MakeRegionKey(frame));
                    frames.Add(frame);
                }

                return new ObjectSprite.Packed(new SpriteAtlasSource.AnimatedSprite(regions, frames, animation.Metadata, anchor));
            }
            else
            {
                double left = anchor.X - (animation.Metadata.Width / 2.0);
                double top = anchor.Y - animation.Metadata.Height;
                Rect bounds = new(left, top, animation.Metadata.Width, animation.Metadata.Height);

                return new ObjectSprite.Direct(new DirectSprite.Animation(key, anchor, item.Depth, bounds, Opacity: 1.0));
            }
        }

        static AtlasTexture.RegionKey MakeRegionKey(Sprite sprite)
        {
            return new AtlasTexture.RegionKey(sprite.Image, sprite.Image.Width, sprite.Image.Height);
        }

        static bool IsPackableSprite(Sprite sprite)
        {
            return sprite.Image.Width * sprite.Image.Height <= MaxPackedObjectPixels;
        }

        static bool IsPackableAnimation(Animation animation)
        {
            int pixels = 0;

            for (int index = 0; index < animation.Metadata.FrameCount; index++)
            {
                Sprite frame = animation[index];
                pixels += frame.Image.Width * frame.Image.Height;
            }

            return pixels <= MaxPackedObjectPixels;
        }
    }

    private static IReadOnlyList<DirectSprite> MergeDirectSprites(DirectSpriteContent content)
    {
        List<DirectSprite> merged = new(content.ObjectSprites.Count + content.ShieldSprites.Count);
        int objectCursor = 0;
        int shieldCursor = 0;

        while (objectCursor < content.ObjectSprites.Count && shieldCursor < content.ShieldSprites.Count)
        {
            if (content.ObjectSprites[objectCursor].Depth <= content.ShieldSprites[shieldCursor].Depth)
            {
                merged.Add(content.ObjectSprites[objectCursor]);
                objectCursor++;
            }
            else
            {
                merged.Add(content.ShieldSprites[shieldCursor]);
                shieldCursor++;
            }
        }

        while (objectCursor < content.ObjectSprites.Count)
        {
            merged.Add(content.ObjectSprites[objectCursor]);
            objectCursor++;
        }

        while (shieldCursor < content.ShieldSprites.Count)
        {
            merged.Add(content.ShieldSprites[shieldCursor]);
            shieldCursor++;
        }

        return merged;
    }

    private static StackLayer BuildStackLayer(IReadOnlyList<MapObject> objects, IAssetProvider assets)
    {
        using CleanupList leases = new();

        List<StackSprite> stacks = [];
        List<DirectSprite> sprites = [];

        foreach (MapObject item in objects)
        {
            switch (item)
            {
                case Stack stack:
                    stacks.Add(new StackSprite(stack, GetStackBounds(stack, assets, leases)));
                    break;

                case CityShield cityShield:
                    Sprite cityShieldSprite = leases.Add(assets.GetRequiredSprite(cityShield.AssetKey)).Value;
                    Point center = Footprint.GetAnchor(cityShield.Footprint);
                    sprites.Add(new DirectSprite.Sprite(cityShield.AssetKey, center, cityShield.Depth, GetSpriteBounds(center, cityShieldSprite), Opacity: 1.0));
                    break;
            }
        }

        return new StackLayer(stacks, sprites);
    }

    private static Rect GetStackBounds(Stack stack, IAssetProvider assets, CleanupList leases)
    {
        Point center = WorldTransform.ToWorldPoint(stack.Position);
        Animation stackAnimation = leases.Add(assets.GetRequiredAnimation(stack.AssetKey)).Value;
        Sprite bannerSprite = leases.Add(assets.GetRequiredSprite(stack.BannerKey)).Value;
        Rect bounds = GetFrameBounds(center, stackAnimation.Metadata);

        bounds = bounds.Union(GetSpriteBounds(center, bannerSprite));

        if (stack.ShadowKey is not null)
        {
            Animation shadowAnimation = leases.Add(assets.GetRequiredAnimation(stack.ShadowKey)).Value;
            bounds = bounds.Union(GetFrameBounds(center, shadowAnimation.Metadata));
        }

        if (stack.AuraKey is not null)
        {
            Animation auraAnimation = leases.Add(assets.GetRequiredAnimation(stack.AuraKey)).Value;
            bounds = bounds.Union(GetFrameBounds(center, auraAnimation.Metadata));
        }

        return bounds;
    }

    private static Rect GetFrameBounds(Point center, AssetAnimationMetadata metadata)
    {
        return new Rect(center.X + metadata.OffsetX, center.Y + metadata.OffsetY, metadata.Width, metadata.Height);
    }

    private static Rect GetSpriteBounds(Point center, Sprite sprite)
    {
        double height = sprite.Image.Height * (sprite.Metadata.Width / (double)sprite.Image.Width);

        return new Rect(center.X + sprite.Metadata.OffsetX, center.Y + sprite.Metadata.OffsetY, sprite.Metadata.Width, height);
    }

    private static IReadOnlyList<HitTarget> BuildHitTargets(IReadOnlyList<MapObject> objects, IAssetMetadataProvider assets)
    {
        List<HitTarget> targets = [];

        foreach (MapObject item in objects)
        {
            if (item is Stack stack)
            {
                AssetAnimationMetadata metadata = assets.GetRequiredAnimationMetadata(stack.AssetKey);
                Point anchor = Tile.GetAnchor(stack.Position, metadata);
                Rect bounds = new(anchor.X - (metadata.Width / 2.0), anchor.Y - metadata.Height, metadata.Width, metadata.Height);
                targets.Add(new HitTarget(stack.Position, bounds));
            }
        }

        return targets;
    }

    private static SKImage BuildMinimapTerrainImage(
        MapTerrain terrain,
        IReadOnlyList<GroundLayer> groundLayers,
        WaterLayer? water,
        SpriteAtlas coastAtlas,
        SpriteAtlas borderAtlas,
        SpriteAtlas roadAtlas)
    {
        MinimapLayout layout = GetMinimapLayout(terrain.Map.Width, terrain.Map.Height);
        int width = Math.Max(1, (int)Math.Ceiling(layout.Box.Width));
        int height = Math.Max(1, (int)Math.Ceiling(layout.Box.Height));

        SKImageInfo info = new(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        SKCanvas canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        Rect visible = GetMinimapBounds(terrain.Map.Width, terrain.Map.Height);

        canvas.Save();
        PushMinimapTransform(canvas, layout);

        using (SKPaint paint = new() { IsAntialias = false })
        {
            Water.Draw(canvas, water, TimeSpan.Zero, zoom: 0, frameIndex: 0);
            DrawAtlasFrame(canvas, paint, coastAtlas.Source, frameIndex: 0, visible);

            foreach (GroundLayer layer in groundLayers)
            {
                paint.Shader = layer.TerrainShader;
                canvas.DrawVertices(layer.Mesh, SKBlendMode.Modulate, paint);
            }

            paint.Shader = null;
            DrawAtlasFrame(canvas, paint, borderAtlas.Source, frameIndex: 0, visible);
            DrawAtlasFrame(canvas, paint, roadAtlas.Source, frameIndex: 0, visible);
        }

        canvas.Restore();

        return surface.CreateRasterImage();
    }

    private static SKImage BuildMinimapImage(MinimapContent content, IAssetProvider assets)
    {
        MinimapLayout layout = GetMinimapLayout(content.Minimap.MapWidth, content.Minimap.MapHeight);
        int width = Math.Max(1, (int)Math.Ceiling(layout.Box.Width));
        int height = Math.Max(1, (int)Math.Ceiling(layout.Box.Height));

        SKImageInfo info = new(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        SKCanvas canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        using (SKImageFilter blurFilter = SKImageFilter.CreateBlur(MinimapBlurStrength, MinimapBlurStrength))
        using (SKPaint blurPaint = new() { ImageFilter = blurFilter })
        {
            canvas.SaveLayer(blurPaint);

            canvas.DrawImage(content.TerrainImage, 0, 0, RenderSamplingOptions.Nearest);

            canvas.Save();
            PushMinimapTransform(canvas, layout);

            using (SKPaint paint = new() { IsAntialias = false })
            {
                DrawMinimapObjects(canvas, paint, content.Objects, assets, GetMinimapBounds(content.Minimap.MapWidth, content.Minimap.MapHeight));
            }

            canvas.Restore();

            DrawMinimapHiddenTiles(canvas, content.Minimap, layout);

            canvas.Restore();
        }

        return surface.CreateRasterImage();
    }

    private static void DrawAtlasFrame(SKCanvas canvas, SKPaint paint, SpriteAtlasSource source, int frameIndex, Rect visible)
    {
        SpriteAtlas atlas = source.CreateAtlas();
        atlas.UpdateAnimations(frameIndex, visible);
        atlas.Draw(canvas, paint, visible);
    }

    private static void DrawMinimapObjects(SKCanvas canvas, SKPaint paint, ObjectLayer objects, IAssetProvider assets, Rect visible)
    {
        SpriteAtlas atlas = objects.Atlas.Source.CreateAtlas();

        atlas.UpdateAnimations(frameIndex: 0, visible);

        int packedCount = atlas.SpriteCount;
        int directCount = objects.DirectSprites.Count;
        int packedCursor = 0;
        int directCursor = 0;

        while (packedCursor < packedCount || directCursor < directCount)
        {
            int directDepth = directCursor < directCount ? objects.DirectSprites[directCursor].Depth : int.MaxValue;

            if (packedCursor < packedCount && objects.SpriteDepths[packedCursor] <= directDepth)
            {
                int start = packedCursor;

                while (packedCursor < packedCount && objects.SpriteDepths[packedCursor] <= directDepth)
                {
                    packedCursor++;
                }

                atlas.DrawRange(canvas, paint, visible, start, packedCursor - start);
            }
            else
            {
                DirectSprite sprite = objects.DirectSprites[directCursor];

                if (visible.Intersects(sprite.Bounds))
                {
                    WorldDirectSpriteRenderer.Draw(canvas, paint, assets, sprite, frameIndex: 0);
                }

                directCursor++;
            }
        }
    }

    private static MinimapLayout GetMinimapLayout(int mapWidth, int mapHeight)
    {
        double width = Math.Max(2, (mapWidth + mapHeight + 2) * MinimapNativeHalfWidth);

        return MinimapLayout.Create(mapWidth, mapHeight, new Rect(0, 0, width, width / 2.0));
    }

    private static Rect GetMinimapBounds(int mapWidth, int mapHeight)
    {
        double left = -mapHeight * WorldTransform.TileHalfWidth;
        double right = mapWidth * WorldTransform.TileHalfWidth;
        double top = -WorldTransform.TileHalfHeight - MinimapCullMargin;
        double bottom = (mapWidth + mapHeight) * WorldTransform.TileHalfHeight;

        return new Rect(left, top, right - left, bottom - top);
    }

    private static void PushMinimapTransform(SKCanvas canvas, MinimapLayout layout)
    {
        Point origin = layout.CellToScreen(0, 0);

        canvas.Translate((float)origin.X, (float)origin.Y);
        canvas.Scale((float)(layout.HalfWidth / WorldTransform.TileHalfWidth), (float)(layout.HalfHeight / WorldTransform.TileHalfHeight));
    }

    private static void DrawMinimapHiddenTiles(SKCanvas canvas, MinimapState minimap, MinimapLayout layout)
    {
        float halfWidth = (float)(layout.HalfWidth + MinimapTileOverlap);
        float halfHeight = (float)(layout.HalfHeight + (MinimapTileOverlap / 2.0));
        SKPathBuilder builder = new();

        foreach (WorldPosition position in minimap.HiddenPositions)
        {
            Point center = layout.CellToScreen(position.X, position.Y);
            float x = (float)center.X;
            float y = (float)center.Y;

            builder.MoveTo(x, y - halfHeight);
            builder.LineTo(x + halfWidth, y);
            builder.LineTo(x, y + halfHeight);
            builder.LineTo(x - halfWidth, y);
            builder.Close();
        }

        using SKPath path = builder.Detach();
        using SKPaint paint = new() { Color = SKColors.Black, BlendMode = SKBlendMode.DstOut };
        canvas.DrawPath(path, paint);
    }
}
