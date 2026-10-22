using Gallean.Common;
using Gallean.Engine.Contracts;
using static Gallean.Assets.AssetSpriteKey.RoadConnections;
using static GameContent.GameAssetDescriptor;
using static GameContent.GameResourceNames;

namespace GameContent;

internal sealed class GameTerrainSource(
    GameImages ground,
    GameImages groundBorders,
    GameImages terrain,
    GameImages animations,
    GameImages common)
{
    public static IEnumerable<GameAssetDescriptor> ListDescriptors()
    {
        foreach (WorldMapTerrain terrainType in Enum.GetValues<WorldMapTerrain>())
        {
            AssetSpriteKey.Ground key = new(terrainType);

            yield return RequiredSprite(
                key,
                source => source.Terrain.GetGround(key.Terrain));
        }

        yield return RequiredAnimation(
            new AssetAnimationKey.Water(),
            source => AnimationOperations.TryCreate(source.Terrain.GetWaterFrames()));

        foreach (AssetSpriteKey.GroundBorderSet set in Enum.GetValues<AssetSpriteKey.GroundBorderSet>())
        {
            for (int edges = 1; edges <= (int)AssetSpriteKey.GroundBorderEdges.All; edges++)
            {
                AssetSpriteKey.GroundBorderShape shape = new AssetSpriteKey.GroundBorderShape.Edges((AssetSpriteKey.GroundBorderEdges)edges);

                foreach (GameAssetDescriptor descriptor in ListGroundBorderDescriptors(set, shape))
                {
                    yield return descriptor;
                }
            }

            for (int corners = 1; corners <= (int)AssetSpriteKey.GroundBorderCorners.All; corners++)
            {
                AssetSpriteKey.GroundBorderShape shape = new AssetSpriteKey.GroundBorderShape.Corners((AssetSpriteKey.GroundBorderCorners)corners);

                foreach (GameAssetDescriptor descriptor in ListGroundBorderDescriptors(set, shape))
                {
                    yield return descriptor;
                }
            }
        }

        foreach (WorldMapTerrain terrainType in Enum.GetValues<WorldMapTerrain>())
        {
            if (terrainType != WorldMapTerrain.Water)
            {
                for (int variant = 0; variant < AssetSpriteKey.Tree.GetVariantCount(terrainType); variant++)
                {
                    AssetSpriteKey.Tree key = new(terrainType, variant);

                    yield return RequiredSprite(key, source => source.Terrain.GetTree(key.Terrain, key.Variant));
                }
            }
        }

        foreach (WorldRace race in Enum.GetValues<WorldRace>())
        {
            foreach (int size in AssetSpriteKey.Mountain.Sizes)
            {
                for (int variant = 0; variant < AssetSpriteKey.Mountain.GetVariantCount(size); variant++)
                {
                    AssetSpriteKey.Mountain key = new(race, size, variant);

                    yield return RequiredSprite(key, source => source.Terrain.GetMountain(key.Race, key.Size, key.Variant));
                }
            }
        }

        foreach (int size in AssetAnimationKey.MountainRemoveEffect.Sizes)
        {
            AssetAnimationKey.MountainRemoveEffect key = new(size);

            yield return RequiredAnimation(key, source => AnimationOperations.TryCreate(source.Terrain.GetMountainRemoveFrames(key.Size)));
        }

        for (int connections = 0; connections <= (int)All; connections++)
        {
            AssetSpriteKey.Road key = new((AssetSpriteKey.RoadConnections)connections);

            yield return RequiredSprite(key, source => source.Terrain.GetRoad(key.Connections));
        }
    }

    private static IEnumerable<GameAssetDescriptor> ListGroundBorderDescriptors(
        AssetSpriteKey.GroundBorderSet set,
        AssetSpriteKey.GroundBorderShape shape)
    {
        for (int variant = 0; variant < AssetSpriteKey.GroundBorder.GetVariantCount(set, shape); variant++)
        {
            AssetSpriteKey.GroundBorder key = new(set, shape, variant);

            yield return RequiredSprite(key, source => source.Terrain.GetGroundBorder(key.Set, key.Shape, key.Variant));
        }
    }

    private AssetSprite? GetGround(WorldMapTerrain terrainType)
    {
        int sheetCount = terrainType == WorldMapTerrain.Water ? 1 : 2;
        List<AssetSprite> sheets = [];

        for (int sheet = 0; sheet < sheetCount * sheetCount; sheet++)
        {
            AssetSprite? sheetSprite = ground.TryGetSprite(Terrain.GroundSheet(terrainType, sheet));

            if (sheetSprite is null)
            {
                return null;
            }

            sheets.Add(sheetSprite);
        }

        int sheetSize = sheets[0].Metadata.Width;
        int resultSize = sheetSize * sheetCount;
        AssetSpriteMetadata metadata = new(resultSize, resultSize, resultSize, resultSize, 0, 0, false);
        AssetSprite result = new(metadata, new byte[resultSize * resultSize * 4]);

        for (int index = 0; index < sheets.Count; index++)
        {
            int column = index % sheetCount;
            int row = index / sheetCount;

            result = SpriteOperations.SourceOver(sheets[index], result, column * sheetSize, row * sheetSize);
        }

        return result;
    }

    private IReadOnlyList<AssetSprite>? GetWaterFrames()
    {
        if (animations.TryGetAnimationFrames(Terrain.WaterAnimation) is { Count: > 0 } sheets)
        {
            return sheets;
        }
        else
        {
            return null;
        }
    }

    private AssetSprite? GetGroundBorder(AssetSpriteKey.GroundBorderSet set, AssetSpriteKey.GroundBorderShape shape, int variant)
    {
        return groundBorders.TryGetSprite(Terrain.GroundBorder(set, GetBorderImageIndex(shape), variant));
    }

    internal static int GetBorderImageIndex(AssetSpriteKey.GroundBorderShape shape)
    {
        switch (shape)
        {
            case AssetSpriteKey.GroundBorderShape.Edges edges:
                int edgeIndex = 0;

                if (edges.Value.HasFlag(AssetSpriteKey.GroundBorderEdges.West))
                {
                    edgeIndex += 1;
                }

                if (edges.Value.HasFlag(AssetSpriteKey.GroundBorderEdges.North))
                {
                    edgeIndex += 2;
                }

                if (edges.Value.HasFlag(AssetSpriteKey.GroundBorderEdges.East))
                {
                    edgeIndex += 4;
                }

                if (edges.Value.HasFlag(AssetSpriteKey.GroundBorderEdges.South))
                {
                    edgeIndex += 8;
                }

                return edgeIndex;

            case AssetSpriteKey.GroundBorderShape.Corners corners:
                int cornerIndex = 16;

                if (corners.Value.HasFlag(AssetSpriteKey.GroundBorderCorners.NorthWest))
                {
                    cornerIndex += 1;
                }

                if (corners.Value.HasFlag(AssetSpriteKey.GroundBorderCorners.NorthEast))
                {
                    cornerIndex += 2;
                }

                if (corners.Value.HasFlag(AssetSpriteKey.GroundBorderCorners.SouthEast))
                {
                    cornerIndex += 4;
                }

                if (corners.Value.HasFlag(AssetSpriteKey.GroundBorderCorners.SouthWest))
                {
                    cornerIndex += 8;
                }

                return cornerIndex;

            default:
                throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
        }
    }

    private AssetSprite? GetTree(WorldMapTerrain terrainType, int variant)
    {
        return PickVariant(terrain.GetObjectSeries(Terrain.Tree(terrainType)), variant);
    }

    private AssetSprite? GetMountain(WorldRace race, int size, int image)
    {
        return terrain.TryGetAnchoredSprite(Terrain.Mountain(race, size, image));
    }

    private IReadOnlyList<AssetSprite>? GetMountainRemoveFrames(int size)
    {
        IReadOnlyList<AssetSprite>? frames = common.TryGetAnimationFrames(Terrain.MountainRemove(size));

        return frames is { Count: > 0 } ? SpriteOperations.CropFramesToContent(frames, SpriteOperations.ContentOrigin.Center, margin: 0) : null;
    }

    private AssetSprite? GetRoad(AssetSpriteKey.RoadConnections connections)
    {
        return terrain.TryGetSprite(Terrain.Road(GetRoadImageIndex(connections)));
    }

    internal static int GetRoadImageIndex(AssetSpriteKey.RoadConnections connections)
    {
        return connections switch
        {
            None => 0,
            All => 1,
            East | West => 2,
            North | South => 3,
            North | South | West => 4,
            North | East | West => 5,
            East | South | West => 6,
            North | East | South => 7,
            North | East => 8,
            East | South => 9,
            South | West => 10,
            North | West => 11,
            East => 12,
            South => 13,
            North => 14,
            West => 15
        };
    }

    private static AssetSprite? PickVariant(IReadOnlyList<AssetSprite> sprites, int variant)
    {
        if (sprites.Count > 0)
        {
            return sprites[variant.Mod(sprites.Count)];
        }
        else
        {
            return null;
        }
    }
}
