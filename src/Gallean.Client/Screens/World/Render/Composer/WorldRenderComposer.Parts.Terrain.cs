using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private static class Terrain
    {
        public readonly record struct Input(WorldMap Map);

        private static readonly WorldMapTerrain[] TerrainsByBlendOrder = [.. Enum.GetValues<WorldMapTerrain>().OrderBy(GetBlendOrder)];

        public static MapTerrain Build(Input input)
        {
            WorldMap map = input.Map;
            List<MapTile> mapTiles = new(map.Width * map.Height);
            List<MapTileBorder> borders = [];
            HashSet<WorldMapTerrain> groundTerrains = [];

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    WorldPosition position = new(x, y);
                    WorldMapCell cell = map.GetCellAt(position);
                    MapTileSurface surface;
                    AssetSpriteKey? roadKey;

                    if (cell.Terrain == WorldMapTerrain.Water)
                    {
                        surface = new MapTileSurface.Water();
                        roadKey = null;
                        AddCoastBorders(map, borders, position);
                    }
                    else
                    {
                        surface = new MapTileSurface.Ground(cell.Terrain);
                        groundTerrains.Add(cell.Terrain);
                        AddBlendBorders(map, borders, position, cell.Terrain);

                        if (cell.Feature == WorldMapFeature.Road)
                        {
                            roadKey = new AssetSpriteKey.Road(GetRoadConnections(map, position));
                        }
                        else
                        {
                            roadKey = null;
                        }
                    }

                    mapTiles.Add(new MapTile(position, surface, roadKey));
                }
            }

            List<WorldMapTerrain> groundTerrainsByBlendOrder = [];

            foreach (WorldMapTerrain terrain in TerrainsByBlendOrder)
            {
                if (groundTerrains.Contains(terrain))
                {
                    groundTerrainsByBlendOrder.Add(terrain);
                }
            }

            return new MapTerrain(map, mapTiles, groundTerrainsByBlendOrder, borders);
        }

        private static int SelectBorderVariant(WorldPosition position)
        {
            return StableHash.Combine(position.X, position.Y);
        }

        private static void AddCoastBorders(WorldMap map, List<MapTileBorder> borders, WorldPosition position)
        {
            (_, AssetSpriteKey.GroundBorderCorners coastCorners) =
                GetBorderConnections(map, position, matchTerrain: null);

            foreach (WorldMapTerrain terrain in TerrainsByBlendOrder)
            {
                if (terrain != WorldMapTerrain.Water)
                {
                    (AssetSpriteKey.GroundBorderEdges edges, AssetSpriteKey.GroundBorderCorners corners) =
                        GetBorderConnections(map, position, terrain);

                    corners &= coastCorners;

                    if (edges != AssetSpriteKey.GroundBorderEdges.None)
                    {
                        borders.Add(new MapTileBorder.Coast(
                            position,
                            new AssetSpriteKey.GroundBorder(
                                AssetSpriteKey.GroundBorderSet.Coast,
                                new AssetSpriteKey.GroundBorderShape.Edges(edges),
                                SelectBorderVariant(position)),
                            terrain,
                            new AssetSpriteKey.GroundBorder(
                                AssetSpriteKey.GroundBorderSet.Mask,
                                new AssetSpriteKey.GroundBorderShape.Edges(edges),
                                SelectBorderVariant(position))));
                    }

                    if (corners != AssetSpriteKey.GroundBorderCorners.None)
                    {
                        borders.Add(new MapTileBorder.Coast(
                            position,
                            new AssetSpriteKey.GroundBorder(
                                AssetSpriteKey.GroundBorderSet.Coast,
                                new AssetSpriteKey.GroundBorderShape.Corners(corners),
                                SelectBorderVariant(position)),
                            terrain,
                            new AssetSpriteKey.GroundBorder(
                                AssetSpriteKey.GroundBorderSet.Mask,
                                new AssetSpriteKey.GroundBorderShape.Corners(corners),
                                SelectBorderVariant(position))));
                    }
                }
            }
        }

        private static void AddBlendBorders(WorldMap map, List<MapTileBorder> borders, WorldPosition position, WorldMapTerrain terrain)
        {
            foreach (WorldMapTerrain neighborTerrain in TerrainsByBlendOrder)
            {
                if (neighborTerrain != terrain &&
                    neighborTerrain != WorldMapTerrain.Water &&
                    GetBlendOrder(neighborTerrain) > GetBlendOrder(terrain))
                {
                    (AssetSpriteKey.GroundBorderEdges edges, AssetSpriteKey.GroundBorderCorners corners) =
                        GetBorderConnections(map, position, neighborTerrain);

                    if (edges != AssetSpriteKey.GroundBorderEdges.None)
                    {
                        borders.Add(new MapTileBorder.Blend(
                            position,
                            new AssetSpriteKey.GroundBorder(
                                AssetSpriteKey.GroundBorderSet.Mask,
                                new AssetSpriteKey.GroundBorderShape.Edges(edges),
                                SelectBorderVariant(position)),
                            neighborTerrain));
                    }

                    if (corners != AssetSpriteKey.GroundBorderCorners.None)
                    {
                        borders.Add(new MapTileBorder.Blend(
                            position,
                            new AssetSpriteKey.GroundBorder(
                                AssetSpriteKey.GroundBorderSet.Mask,
                                new AssetSpriteKey.GroundBorderShape.Corners(corners),
                                SelectBorderVariant(position)),
                            neighborTerrain));
                    }
                }
            }
        }

        private static int GetBlendOrder(WorldMapTerrain terrain)
        {
            return terrain switch
            {
                WorldMapTerrain.Dwarf => 6,
                WorldMapTerrain.Elf => 5,
                WorldMapTerrain.Human => 4,
                WorldMapTerrain.Undead => 3,
                WorldMapTerrain.Heretic => 2,
                WorldMapTerrain.Neutral => 1,
                WorldMapTerrain.Water => 0
            };
        }

        private static bool Matches(WorldMap map, WorldPosition position, WorldMapTerrain? matchTerrain)
        {
            if (matchTerrain is null)
            {
                return IsLand(map, position);
            }
            else
            {
                return TryGetTerrainAt(map, position) == matchTerrain;
            }
        }

        private static (AssetSpriteKey.GroundBorderEdges Edges, AssetSpriteKey.GroundBorderCorners Corners) GetBorderConnections(
            WorldMap map,
            WorldPosition position,
            WorldMapTerrain? matchTerrain)
        {
            bool north = Matches(map, position with { Y = position.Y - 1 }, matchTerrain);
            bool east = Matches(map, position with { X = position.X + 1 }, matchTerrain);
            bool south = Matches(map, position with { Y = position.Y + 1 }, matchTerrain);
            bool west = Matches(map, position with { X = position.X - 1 }, matchTerrain);

            AssetSpriteKey.GroundBorderEdges edges = AssetSpriteKey.GroundBorderEdges.None;

            if (north)
            {
                edges |= AssetSpriteKey.GroundBorderEdges.North;
            }

            if (east)
            {
                edges |= AssetSpriteKey.GroundBorderEdges.East;
            }

            if (south)
            {
                edges |= AssetSpriteKey.GroundBorderEdges.South;
            }

            if (west)
            {
                edges |= AssetSpriteKey.GroundBorderEdges.West;
            }

            AssetSpriteKey.GroundBorderCorners corners = AssetSpriteKey.GroundBorderCorners.None;

            if (!north && !west && Matches(map, new WorldPosition(position.X - 1, position.Y - 1), matchTerrain))
            {
                corners |= AssetSpriteKey.GroundBorderCorners.NorthWest;
            }

            if (!north && !east && Matches(map, new WorldPosition(position.X + 1, position.Y - 1), matchTerrain))
            {
                corners |= AssetSpriteKey.GroundBorderCorners.NorthEast;
            }

            if (!south && !east && Matches(map, new WorldPosition(position.X + 1, position.Y + 1), matchTerrain))
            {
                corners |= AssetSpriteKey.GroundBorderCorners.SouthEast;
            }

            if (!south && !west && Matches(map, new WorldPosition(position.X - 1, position.Y + 1), matchTerrain))
            {
                corners |= AssetSpriteKey.GroundBorderCorners.SouthWest;
            }

            return (edges, corners);
        }

        private static bool IsLand(WorldMap map, WorldPosition position)
        {
            WorldMapTerrain? terrain = TryGetTerrainAt(map, position);

            return terrain is not null && terrain != WorldMapTerrain.Water;
        }

        private static WorldMapTerrain? TryGetTerrainAt(WorldMap map, WorldPosition position)
        {
            if (map.IsValidPosition(position))
            {
                return map.GetCellAt(position).Terrain;
            }
            else
            {
                return null;
            }
        }

        private static AssetSpriteKey.RoadConnections GetRoadConnections(WorldMap map, WorldPosition position)
        {
            AssetSpriteKey.RoadConnections connections = AssetSpriteKey.RoadConnections.None;

            if (HasRoad(map, position.X, position.Y - 1))
            {
                connections |= AssetSpriteKey.RoadConnections.North;
            }

            if (HasRoad(map, position.X + 1, position.Y))
            {
                connections |= AssetSpriteKey.RoadConnections.East;
            }

            if (HasRoad(map, position.X, position.Y + 1))
            {
                connections |= AssetSpriteKey.RoadConnections.South;
            }

            if (HasRoad(map, position.X - 1, position.Y))
            {
                connections |= AssetSpriteKey.RoadConnections.West;
            }

            return connections;
        }

        private static bool HasRoad(WorldMap map, int x, int y)
        {
            WorldPosition position = new(x, y);

            return map.IsValidPosition(position) &&
                   map.GetCellAt(position).Feature == WorldMapFeature.Road;
        }
    }
}
