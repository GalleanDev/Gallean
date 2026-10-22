using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Gallean.Common;

namespace Gallean.Engine;

public sealed class WorldPlan
{
    private readonly record struct ObjectiveKey(
        IGameScenarioProvider Scenario,
        WorldPlayerId PlayerId);

    private readonly record struct PathMapKey(
        WorldPosition Start,
        ImmutableBitmap? FogOfWar,
        WorldMovementCalculator.MovementProfile Profile);

    private static readonly ConditionalWeakTable<WorldState, WorldPlan> Cache = [];

    private readonly Lazy<Dictionary<WorldPosition, WorldBag>> bags;
    private readonly Dictionary<WorldMovementCalculator.MovementProfile, ImmutableBitmap> blockedCells;
    private readonly Lazy<Dictionary<WorldPlayerId, WorldCapital>> capitals;
    private readonly Lazy<HashSet<WorldPlayerId>> defeatedPlayers;
    private readonly Lazy<Dictionary<WorldPosition, WorldFortification>> fortifications;
    private readonly Lazy<Dictionary<WorldPosition, WorldLandmark>> landmarks;
    private readonly Lazy<ImmutableBitmap> obstacleCells;
    private readonly Dictionary<ObjectiveKey, ImmutableArray<WorldObjective>> objectives;
    private readonly Dictionary<PathMapKey, WorldPathMap?> pathMaps;
    private readonly Lazy<Dictionary<WorldPosition, WorldResourceNode>> resourceNodes;
    private readonly Lazy<Dictionary<WorldPosition, WorldRod>> rods;
    private readonly Lazy<Dictionary<WorldPosition, WorldSite>> sites;
    private readonly Lazy<Dictionary<WorldPosition, WorldStack>> stacks;
    private readonly WorldState state;
    private readonly Lazy<ImmutableBitmap> structureCells;
    private readonly Lazy<ImmutableBitmap> terminalCells;
    private readonly Lazy<Dictionary<WorldPosition, ImmutableArray<WorldTomb>>> tombs;
    private readonly Lazy<ImmutableBitmap> zeroCostEntrances;

    private WorldPlan(WorldState state)
    {
        this.state = state;

        bags = new Lazy<Dictionary<WorldPosition, WorldBag>>(BuildBags);
        blockedCells = [];
        capitals = new Lazy<Dictionary<WorldPlayerId, WorldCapital>>(BuildCapitals);
        defeatedPlayers = new Lazy<HashSet<WorldPlayerId>>(BuildDefeatedPlayers);
        fortifications = new Lazy<Dictionary<WorldPosition, WorldFortification>>(BuildFortifications);
        landmarks = new Lazy<Dictionary<WorldPosition, WorldLandmark>>(BuildLandmarks);
        objectives = [];
        obstacleCells = new Lazy<ImmutableBitmap>(BuildObstacleCells);
        pathMaps = [];
        resourceNodes = new Lazy<Dictionary<WorldPosition, WorldResourceNode>>(BuildResourceNodes);
        rods = new Lazy<Dictionary<WorldPosition, WorldRod>>(BuildRods);
        sites = new Lazy<Dictionary<WorldPosition, WorldSite>>(BuildSites);
        stacks = new Lazy<Dictionary<WorldPosition, WorldStack>>(BuildStacks);
        structureCells = new Lazy<ImmutableBitmap>(BuildStructureCells);
        terminalCells = new Lazy<ImmutableBitmap>(BuildTerminalCells);
        tombs = new Lazy<Dictionary<WorldPosition, ImmutableArray<WorldTomb>>>(BuildTombs);
        zeroCostEntrances = new Lazy<ImmutableBitmap>(BuildZeroCostEntrances);
    }

    public static WorldPlan For(WorldState state)
    {
        return Cache.GetValue(state, static s => new WorldPlan(s));
    }

    public WorldStack? GetStackAt(WorldPosition position)
    {
        stacks.Value.TryGetValue(position, out WorldStack? stack);

        return stack;
    }

    public WorldFortification? GetFortificationAt(WorldPosition position)
    {
        fortifications.Value.TryGetValue(position, out WorldFortification? fortification);

        return fortification;
    }

    public WorldCapital? GetCapital(WorldPlayerId playerId)
    {
        capitals.Value.TryGetValue(playerId, out WorldCapital? capital);

        return capital;
    }

    public bool IsPlayerDefeated(WorldPlayerId playerId)
    {
        return defeatedPlayers.Value.Contains(playerId);
    }

    public ImmutableArray<WorldObjective> GetObjectives(IGameScenarioProvider scenario, WorldPlayerId playerId)
    {
        ObjectiveKey key = new(scenario, playerId);

        if (!objectives.TryGetValue(key, out ImmutableArray<WorldObjective> result))
        {
            result = ObjectiveCalculator.GetObjectives(scenario, state, playerId);

            objectives.Add(key, result);
        }

        return result;
    }

    public WorldResourceNode? GetResourceNodeAt(WorldPosition position)
    {
        resourceNodes.Value.TryGetValue(position, out WorldResourceNode? resourceNode);

        return resourceNode;
    }

    public WorldRod? GetRodAt(WorldPosition position)
    {
        rods.Value.TryGetValue(position, out WorldRod? rod);

        return rod;
    }

    public WorldSite? GetSiteAt(WorldPosition position)
    {
        sites.Value.TryGetValue(position, out WorldSite? site);

        return site;
    }

    public WorldLandmark? GetLandmarkAt(WorldPosition position)
    {
        landmarks.Value.TryGetValue(position, out WorldLandmark? landmark);

        return landmark;
    }

    public bool HasStructureAt(WorldPosition position)
    {
        return structureCells.Value.Get(position.X, position.Y);
    }

    public bool IsObstacleAt(WorldPosition position)
    {
        return obstacleCells.Value.Get(position.X, position.Y);
    }

    public WorldBag? GetBagAt(WorldPosition position)
    {
        bags.Value.TryGetValue(position, out WorldBag? bag);

        return bag;
    }

    public ImmutableArray<WorldTomb> GetTombsAt(WorldPosition position)
    {
        if (tombs.Value.TryGetValue(position, out ImmutableArray<WorldTomb> result))
        {
            return result;
        }
        else
        {
            return [];
        }
    }

    internal bool CanPlaceStackAt(WorldPosition position, WorldMovementCalculator.MovementProfile profile)
    {
        return state.Map.IsValidPosition(position) &&
               GetStackAt(position) is null &&
               GetBagAt(position) is null &&
               GetRodAt(position) is null &&
               !HasStructureAt(position) &&
               WorldMovementCalculator.IsPassable(state.Map, position, profile);
    }

    internal int GetStepCost(WorldPosition from, WorldPosition to, WorldMovementCalculator.MovementProfile profile)
    {
        int cost = WorldMovementCalculator.GetMovementCost(state.Map, to, profile);

        if (cost >= int.MaxValue)
        {
            return int.MaxValue;
        }

        ImmutableBitmap entrances = zeroCostEntrances.Value;

        if (entrances.Get(from.X, from.Y) || entrances.Get(to.X, to.Y))
        {
            return 0;
        }

        return cost;
    }

    private Dictionary<WorldPosition, WorldStack> BuildStacks()
    {
        Dictionary<WorldPosition, WorldStack> result = new(state.Stacks.Count);

        foreach (WorldStack stack in state.Stacks.Values)
        {
            result.Add(stack.Position, stack);
        }

        return result;
    }

    private Dictionary<WorldPosition, WorldFortification> BuildFortifications()
    {
        Dictionary<WorldPosition, WorldFortification> result = [];

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            foreach (WorldPosition tile in fortification.Footprint.EnumerateTiles())
            {
                result[tile] = fortification;
            }
        }

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            result[fortification.Entrance] = fortification;
        }

        return result;
    }

    private Dictionary<WorldPosition, WorldResourceNode> BuildResourceNodes()
    {
        Dictionary<WorldPosition, WorldResourceNode> result = new(state.ResourceNodes.Count);

        foreach (WorldResourceNode resourceNode in state.ResourceNodes.Values)
        {
            result.Add(resourceNode.Position, resourceNode);
        }

        return result;
    }

    private Dictionary<WorldPosition, WorldRod> BuildRods()
    {
        Dictionary<WorldPosition, WorldRod> result = new(state.Rods.Count);

        foreach (WorldRod rod in state.Rods.Values)
        {
            result.Add(rod.Position, rod);
        }

        return result;
    }

    private Dictionary<WorldPosition, WorldSite> BuildSites()
    {
        Dictionary<WorldPosition, WorldSite> result = [];

        foreach (WorldSite site in state.Sites.Values)
        {
            foreach (WorldPosition tile in site.Footprint.EnumerateTiles())
            {
                result[tile] = site;
            }
        }

        foreach (WorldSite site in state.Sites.Values)
        {
            result[site.Entrance] = site;
        }

        return result;
    }

    private Dictionary<WorldPosition, WorldLandmark> BuildLandmarks()
    {
        Dictionary<WorldPosition, WorldLandmark> result = [];

        foreach (WorldLandmark landmark in state.Landmarks.Values)
        {
            foreach (WorldPosition tile in landmark.Footprint.EnumerateTiles())
            {
                result[tile] = landmark;
            }
        }

        return result;
    }

    private ImmutableBitmap BuildStructureCells()
    {
        ImmutableBitmap.Builder structures = ImmutableBitmap.Create(state.Map.Width, state.Map.Height).ToBuilder();

        foreach (WorldResourceNode resourceNode in state.ResourceNodes.Values)
        {
            structures.Set(resourceNode.Position.X, resourceNode.Position.Y);
        }

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            SetFootprint(structures, fortification.Footprint);
        }

        foreach (WorldSite site in state.Sites.Values)
        {
            SetFootprint(structures, site.Footprint);
        }

        foreach (WorldLandmark landmark in state.Landmarks.Values)
        {
            SetFootprint(structures, landmark.Footprint);
        }

        return structures.ToImmutable();
    }

    private static void SetFootprint(ImmutableBitmap.Builder cells, WorldFootprint footprint)
    {
        foreach (WorldPosition tile in footprint.EnumerateTiles())
        {
            cells.Set(tile.X, tile.Y);
        }
    }

    private ImmutableBitmap BuildZeroCostEntrances()
    {
        ImmutableBitmap.Builder entrances = ImmutableBitmap.Create(state.Map.Width, state.Map.Height).ToBuilder();

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            if (fortification is WorldCapital or WorldCity)
            {
                entrances.Set(fortification.Entrance.X, fortification.Entrance.Y);
            }
        }

        return entrances.ToImmutable();
    }

    private ImmutableBitmap BuildTerminalCells()
    {
        ImmutableBitmap.Builder terminals = ImmutableBitmap.Create(state.Map.Width, state.Map.Height).ToBuilder();

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            terminals.Set(fortification.Entrance.X, fortification.Entrance.Y);
        }

        foreach (WorldSite site in state.Sites.Values)
        {
            terminals.Set(site.Entrance.X, site.Entrance.Y);
        }

        foreach (WorldStack stack in state.Stacks.Values)
        {
            terminals.Set(stack.Position.X, stack.Position.Y);
        }

        foreach (WorldBag bag in state.Bags.Values)
        {
            terminals.Set(bag.Position.X, bag.Position.Y);
        }

        return terminals.ToImmutable();
    }

    private Dictionary<WorldPosition, WorldBag> BuildBags()
    {
        Dictionary<WorldPosition, WorldBag> result = new(state.Bags.Count);

        foreach (WorldBag bag in state.Bags.Values)
        {
            result.Add(bag.Position, bag);
        }

        return result;
    }

    private Dictionary<WorldPosition, ImmutableArray<WorldTomb>> BuildTombs()
    {
        Dictionary<WorldPosition, ImmutableArray<WorldTomb>.Builder> builders = [];

        foreach (WorldTomb tomb in state.Tombs.Values)
        {
            if (!builders.TryGetValue(tomb.Position, out ImmutableArray<WorldTomb>.Builder? builder))
            {
                builder = ImmutableArray.CreateBuilder<WorldTomb>();
                builders.Add(tomb.Position, builder);
            }

            builder.Add(tomb);
        }

        Dictionary<WorldPosition, ImmutableArray<WorldTomb>> result = new(builders.Count);

        foreach ((WorldPosition position, ImmutableArray<WorldTomb>.Builder builder) in builders)
        {
            result.Add(position, builder.ToImmutable());
        }

        return result;
    }

    private Dictionary<WorldPlayerId, WorldCapital> BuildCapitals()
    {
        Dictionary<WorldPlayerId, WorldCapital> result = [];

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            if (fortification is WorldCapital { CapturedById: null } capital)
            {
                result.TryAdd(capital.OwnerId, capital);
            }
        }

        return result;
    }

    private HashSet<WorldPlayerId> BuildDefeatedPlayers()
    {
        HashSet<WorldPlayerId> result = [];

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            if (fortification is WorldCapital { CapturedById: not null } capital)
            {
                result.Add(capital.OwnerId);
            }
        }

        result.ExceptWith(capitals.Value.Keys);

        return result;
    }

    internal WorldPath? GetPath(
        WorldPosition start,
        WorldPosition end,
        ImmutableBitmap? fogOfWar,
        WorldMovementCalculator.MovementProfile profile,
        WorldMovementLimit limit)
    {
        WorldPathMap? pathMap = GetPathMap(start, fogOfWar, profile);

        if (pathMap is null)
        {
            return null;
        }

        return pathMap.TryGetPath(end, limit, out WorldPath? path) ? path : null;
    }

    internal WorldPathMap? GetPathMap(
        WorldPosition start,
        ImmutableBitmap? fogOfWar,
        WorldMovementCalculator.MovementProfile profile)
    {
        PathMapKey key = new(start, fogOfWar, profile);

        if (!pathMaps.TryGetValue(key, out WorldPathMap? pathMap))
        {
            pathMap = PathMapCalculator.Calculate(
                state.Map,
                start,
                fogOfWar,
                GetBlockedCells(profile),
                terminalCells.Value,
                (from, to) => GetStepCost(from, to, profile));

            pathMaps.Add(key, pathMap);
        }

        return pathMap;
    }

    private ImmutableBitmap GetBlockedCells(WorldMovementCalculator.MovementProfile profile)
    {
        if (!blockedCells.TryGetValue(profile, out ImmutableBitmap? cells))
        {
            cells = BuildBlockedCells(profile);
            blockedCells.Add(profile, cells);
        }

        return cells;
    }

    private ImmutableBitmap BuildObstacleCells()
    {
        ImmutableBitmap.Builder cells = ImmutableBitmap.Create(state.Map.Width, state.Map.Height).ToBuilder();

        foreach (WorldResourceNode resourceNode in state.ResourceNodes.Values)
        {
            cells.Set(resourceNode.Position.X, resourceNode.Position.Y);
        }

        foreach (WorldRod rod in state.Rods.Values)
        {
            cells.Set(rod.Position.X, rod.Position.Y);
        }

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            BlockFootprint(cells, fortification.Footprint, fortification.Entrance);
        }

        foreach (WorldSite site in state.Sites.Values)
        {
            BlockFootprint(cells, site.Footprint, site.Entrance);
        }

        foreach (WorldLandmark landmark in state.Landmarks.Values)
        {
            foreach (WorldPosition tile in landmark.Footprint.EnumerateTiles())
            {
                cells.Set(tile.X, tile.Y);
            }
        }

        for (int y = 0; y < state.Map.Height; y++)
        {
            for (int x = 0; x < state.Map.Width; x++)
            {
                WorldMapCell cell = state.Map.GetCellAt(new WorldPosition(x, y));

                if (cell.Feature == WorldMapFeature.Mountain)
                {
                    cells.Set(x, y);
                }
            }
        }

        return cells.ToImmutable();
    }

    private ImmutableBitmap BuildBlockedCells(WorldMovementCalculator.MovementProfile profile)
    {
        ImmutableBitmap.Builder cells = obstacleCells.Value.ToBuilder();

        for (int y = 0; y < state.Map.Height; y++)
        {
            for (int x = 0; x < state.Map.Width; x++)
            {
                WorldPosition position = new(x, y);

                if (!WorldMovementCalculator.IsPassable(state.Map, position, profile))
                {
                    cells.Set(x, y);
                }
            }
        }

        return cells.ToImmutable();
    }

    private static void BlockFootprint(ImmutableBitmap.Builder cells, WorldFootprint footprint, WorldPosition entrance)
    {
        foreach (WorldPosition tile in footprint.EnumerateTiles())
        {
            if (tile != entrance)
            {
                cells.Set(tile.X, tile.Y);
            }
        }
    }
}
