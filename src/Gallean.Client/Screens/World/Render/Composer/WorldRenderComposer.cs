using System.Collections.Immutable;
using System.Globalization;
using Avalonia.Media;
using Gallean.Common;
using Gallean.Engine;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer(
    IGameDefinitionProvider definitions,
    IGameScenarioProvider scenario,
    IAssetMetadataProvider assets)
{
    private const double CursorScaleSmall = 1.0;
    private const double CursorScaleLarge = 1.3;

    private readonly Part<AmbientSounds.Input, IReadOnlyList<AmbientSound>> ambientSoundsPart = new(AmbientSounds.Build);
    private readonly Part<Bags.Input, IReadOnlyList<MapObject>> bagsPart = new(Bags.Build);
    private readonly Part<Clouds.Input, IReadOnlyList<Cloud>> cloudsPart = new(Clouds.Build);
    private readonly Part<Fog.Input, IReadOnlyList<FogTile>> fogPart = new(Fog.Build);
    private readonly Part<Fortifications.Input, IReadOnlyList<MapObject>> fortificationsPart = new(input => Fortifications.Build(input, assets));
    private readonly Part<Landmarks.Input, IReadOnlyList<MapObject>> landmarksPart = new(Landmarks.Build);
    private readonly Part<MapFeatures.Input, IReadOnlyList<MapObject>> mapFeaturesPart = new(MapFeatures.Build);
    private readonly Part<MapObjects.Input, IReadOnlyList<MapObject>> mapObjectsPart = new(MapObjects.Build);
    private readonly Part<Minimap.Input, MinimapState> minimapPart = new(Minimap.Build);
    private readonly Part<Mountains.Input, IReadOnlyList<MapObject>> mountainsPart = new(Mountains.Build);
    private readonly Part<Objectives.Input, IReadOnlyList<ObjectiveMarker>> objectivesPart = new(Objectives.Build);
    private readonly Part<ResourceNodes.Input, IReadOnlyList<MapObject>> resourceNodesPart = new(ResourceNodes.Build);
    private readonly Part<Rods.Input, IReadOnlyList<MapObject>> rodsPart = new(Rods.Build);
    private readonly Part<Sites.Input, IReadOnlyList<MapObject>> sitesPart = new(input => Sites.Build(input, assets));
    private readonly Part<Stacks.Input, IReadOnlyList<MapObject>> stacksPart = new(input => Stacks.Build(input, definitions, assets));
    private readonly Part<Terrain.Input, MapTerrain> terrainPart = new(Terrain.Build);
    private readonly Part<Tombs.Input, IReadOnlyList<MapObject>> tombsPart = new(Tombs.Build);

    private WorldAnimation? currentAnimation;
    private WorldAnimationState? currentAnimationState;
    private WorldRenderState? previousState;

    public WorldRenderState Compose(WorldRenderRequest request)
    {
        WorldState state = request.State;
        WorldPlan plan = WorldPlan.For(state);
        WorldVisibility visibility = WorldVisibility.Create(state, request.ViewPlayerId, request.FogEnabled);
        PlayerRaces playerRaces = PlayerRaces.Create(state.Players);
        ImmutableArray<WorldObjective> objectives = plan.GetObjectives(scenario, request.ViewPlayerId);
        WorldAnimation? animation = ResolveAnimation(request, objectives);
        WorldStackId? animatedStackId = animation?.Accept(WorldAnimationStackVisitor.Instance, Empty.Value);

        MapTerrain terrain = terrainPart.Resolve(new Terrain.Input(state.Map));
        IReadOnlyList<Cloud> clouds = cloudsPart.Resolve(new Clouds.Input(state.Map.Width, state.Map.Height));
        IReadOnlyList<FogTile> fogTiles = fogPart.Resolve(new Fog.Input(visibility));
        IReadOnlyList<MapObject> landmarks = landmarksPart.Resolve(new Landmarks.Input(state.Landmarks, state.Map, visibility));
        IReadOnlyList<MapObject> mountains = mountainsPart.Resolve(new Mountains.Input(state.Mountains, visibility));
        IReadOnlyList<MapObject> sites = sitesPart.Resolve(new Sites.Input(state.Sites, visibility));
        IReadOnlyList<MapObject> mapFeatures = mapFeaturesPart.Resolve(new MapFeatures.Input(state.Map, visibility));
        IReadOnlyList<MapObject> resourceNodes = resourceNodesPart.Resolve(new ResourceNodes.Input(state.ResourceNodes, visibility));
        IReadOnlyList<MapObject> rods = rodsPart.Resolve(new Rods.Input(state.Rods, playerRaces, visibility));
        IReadOnlyList<MapObject> tombs = tombsPart.Resolve(new Tombs.Input(state.Tombs, state.Map, visibility));
        IReadOnlyList<MapObject> bags = bagsPart.Resolve(new Bags.Input(state.Bags, state.Map, visibility));
        IReadOnlyList<MapObject> fortifications = fortificationsPart.Resolve(new Fortifications.Input(state.Fortifications, playerRaces, visibility));
        IReadOnlyList<MapObject> stacks = stacksPart.Resolve(new Stacks.Input(state.Stacks, state.Subraces, state.Fortifications, playerRaces, state.Map, visibility, request.ViewPlayerId, animatedStackId));
        IReadOnlyList<MapObject> mapObjects = mapObjectsPart.Resolve(new MapObjects.Input(landmarks, mountains, sites, mapFeatures, resourceNodes, rods, tombs, bags, fortifications));
        IReadOnlyList<ObjectiveMarker> objectiveMarkers = objectivesPart.Resolve(new Objectives.Input(objectives, visibility, animatedStackId));
        IReadOnlyList<AmbientSound> ambientSounds = ambientSoundsPart.Resolve(new AmbientSounds.Input(state.Fortifications, state.Landmarks, state.Sites, state.ResourceNodes, playerRaces, visibility));
        MinimapState minimap = minimapPart.Resolve(new Minimap.Input(state.Map, state.Fortifications, state.Stacks, playerRaces, visibility, request.ViewPlayerId, request.FogEnabled));

        ReachOverlay? reach;
        HighlightOverlay highlights;

        if (animation is StackBattle or BattleTransition)
        {
            reach = null;
            highlights = new HighlightOverlay([]);
        }
        else
        {
            reach = BuildReachOverlay(request, animatedStackId);
            highlights = BuildHighlights(request, plan, stacks, fortifications, animatedStackId);
        }

        PathOverlay? selectedPathOverlay = BuildSelectedPathOverlay(request);
        IReadOnlyList<Cursor> cursors = BuildCursors(request, visibility, plan, definitions, selectedPathOverlay);

        WorldRenderState world = BuildWorld(
            terrain,
            mapObjects,
            stacks,
            animation,
            clouds,
            fogTiles,
            objectiveMarkers,
            ambientSounds,
            cursors,
            reach,
            highlights,
            selectedPathOverlay,
            minimap);

        previousState = world;

        return world;
    }

    private WorldRenderState BuildWorld(
        MapTerrain terrain,
        IReadOnlyList<MapObject> mapObjects,
        IReadOnlyList<MapObject> stacks,
        WorldAnimation? animation,
        IReadOnlyList<Cloud> clouds,
        IReadOnlyList<FogTile> fogTiles,
        IReadOnlyList<ObjectiveMarker> objectiveMarkers,
        IReadOnlyList<AmbientSound> ambientSounds,
        IReadOnlyList<Cursor> cursors,
        ReachOverlay? reach,
        HighlightOverlay highlights,
        PathOverlay? selectedPath,
        MinimapState minimap)
    {
        if (previousState is not null &&
            ReferenceEquals(previousState.Terrain, terrain) &&
            ReferenceEquals(previousState.MapObjects, mapObjects) &&
            ReferenceEquals(previousState.Stacks, stacks) &&
            ReferenceEquals(previousState.Animation, animation) &&
            ReferenceEquals(previousState.Clouds, clouds) &&
            ReferenceEquals(previousState.FogTiles, fogTiles) &&
            ReferenceEquals(previousState.ObjectiveMarkers, objectiveMarkers) &&
            ReferenceEquals(previousState.AmbientSounds, ambientSounds) &&
            ReferenceEquals(previousState.Minimap, minimap) &&
            cursors.SequenceEqual(previousState.Cursors) &&
            previousState.Reach == reach &&
            previousState.Highlights == highlights &&
            previousState.SelectedPath == selectedPath)
        {
            return previousState;
        }
        else
        {
            return new WorldRenderState(
                terrain,
                mapObjects,
                stacks,
                animation,
                clouds,
                fogTiles,
                objectiveMarkers,
                ambientSounds,
                cursors,
                reach,
                highlights,
                selectedPath,
                minimap);
        }
    }

    private WorldAnimation? ResolveAnimation(WorldRenderRequest request, ImmutableArray<WorldObjective> objectives)
    {
        if (request.Animation is null)
        {
            currentAnimationState = null;
            currentAnimation = null;

            return null;
        }

        if (!ReferenceEquals(request.Animation, currentAnimationState))
        {
            currentAnimationState = request.Animation;
            currentAnimation = request.Animation switch
            {
                WorldAnimationState.StackMove data => AnimationBuilder.BuildStackMove(
                    request.State.Stacks,
                    request.State.Subraces,
                    request.State.Map,
                    definitions,
                    assets,
                    data.StackId,
                    data.Steps,
                    objectives.Any(objective => objective.StackId == data.StackId),
                    request.Cursor.StackId == data.StackId ? Theme.World.Highlight.SelectedColor : null),
                WorldAnimationState.StackDestroy data => AnimationBuilder.BuildStackDestroy(definitions, assets, data.Position, data.LeaderDefinitionId),
                WorldAnimationState.StackRemove data => AnimationBuilder.BuildStackRemove(definitions, assets, data.Position, data.LeaderDefinitionId),
                WorldAnimationState.StackCreate data => AnimationBuilder.BuildStackCreate(assets, data.Position),
                WorldAnimationState.StackOwnerChange data => AnimationBuilder.BuildStackOwnerChange(assets, data.Position),
                WorldAnimationState.MountainRemove data => AnimationBuilder.BuildMountainRemove(assets, data.Center, data.Radius),
                WorldAnimationState.Pause data => new Pause(data.Duration),
                WorldAnimationState.StackBattle data => AnimationBuilder.BuildStackBattle(assets, data.Position),
                WorldAnimationState.BattleStart data => new BattleTransition(data.Position, BattleTransitionType.In),
                WorldAnimationState.BattleEnd data => new BattleTransition(data.Position, BattleTransitionType.Out),
                WorldAnimationState.SpellCast data => AnimationBuilder.BuildSpellCast(
                    request.State.Stacks,
                    request.State.Subraces,
                    request.State.Map,
                    definitions,
                    assets,
                    data.SpellId,
                    data.Position,
                    data.SummonedStackId),
                _ => throw new ArgumentOutOfRangeException(nameof(request), request.Animation, null)
            };
        }

        return currentAnimation;
    }

    private static IReadOnlyList<Cursor> BuildCursors(
        WorldRenderRequest request,
        WorldVisibility visibility,
        WorldPlan plan,
        IGameDefinitionProvider definitions,
        PathOverlay? path)
    {
        List<Cursor> cursors = [];

        if (request.Cursor.Action is not null)
        {
            cursors.AddRange(BuildActionCursors(request, visibility));
        }
        else
        {
            cursors.AddRange(BuildHoverCursors(request, visibility, plan, definitions, path));
        }

        return [.. cursors.OrderBy(cursor => cursor.Depth)];
    }

    private static double GetCursorScale(WorldStack? stack, IGameDefinitionProvider definitions)
    {
        if (stack is not null)
        {
            (WorldUnit _, GridPosition _, UnitDefinition leaderDefinition) = stack.GetLeader(definitions);

            if (leaderDefinition.IsLarge)
            {
                return CursorScaleLarge;
            }
            else
            {
                return CursorScaleSmall;
            }
        }
        else
        {
            return CursorScaleSmall;
        }
    }

    private static IReadOnlyList<Cursor> BuildActionCursors(WorldRenderRequest request, WorldVisibility visibility)
    {
        WorldPosition? position = request.Cursor.HoverTile;

        if (position is not null &&
            request.State.Map.IsValidPosition(position.Value) &&
            visibility.IsRevealed(position.Value))
        {
            return
            [
                new Cursor(
                    position.Value,
                    GetDepth(position.Value, Layer.TileHighlight),
                    Theme.World.SpellCursorInnerColor,
                    Theme.World.SpellCursorOuterColor,
                    CursorScaleSmall)
            ];
        }
        else
        {
            return [];
        }
    }

    private static IReadOnlyList<Cursor> BuildHoverCursors(
        WorldRenderRequest request,
        WorldVisibility visibility,
        WorldPlan plan,
        IGameDefinitionProvider definitions,
        PathOverlay? path)
    {
        WorldStack? selected = null;

        if (request.Cursor.StackId is not null)
        {
            request.State.Stacks.TryGetValue(request.Cursor.StackId.Value, out selected);
        }

        PathStep? destination = path?.Steps[^1];
        List<WorldPosition> stackTiles = [];

        foreach (WorldPosition stackTile in request.Cursor.HoverObjects)
        {
            if (request.State.Map.IsValidPosition(stackTile) &&
                visibility.IsRevealed(stackTile) &&
                stackTile != selected?.Position &&
                stackTile != destination?.Position &&
                !stackTiles.Contains(stackTile))
            {
                stackTiles.Add(stackTile);
            }
        }

        List<Cursor> cursors = [];

        foreach (WorldPosition stackTile in stackTiles)
        {
            WorldStack? hovered = plan.GetStackAt(stackTile);

            cursors.Add(new Cursor(
                stackTile,
                GetDepth(stackTile, Layer.SymbolStackHighlight),
                Theme.World.HoverCursorInnerColor,
                Theme.World.HoverCursorOuterColor,
                GetCursorScale(hovered, definitions)));
        }

        if (destination is not null && visibility.IsRevealed(destination.Position))
        {
            WorldStack? stack = plan.GetStackAt(destination.Position);
            Layer layer = stack is null ? Layer.TileHighlight : Layer.SymbolStackHighlight;

            cursors.Add(new Cursor(
                destination.Position,
                GetDepth(destination.Position, layer),
                destination.InnerColor,
                destination.OuterColor,
                GetCursorScale(stack, definitions)));
        }

        return cursors;
    }

    private static HighlightOverlay BuildHighlights(
        WorldRenderRequest request,
        WorldPlan plan,
        IReadOnlyList<MapObject> stacks,
        IReadOnlyList<MapObject> fortifications,
        WorldStackId? animatedStackId)
    {
        WorldCursor cursor = request.Cursor;
        List<Highlight> highlights = [];
        WorldStack? selected = null;

        if (cursor.StackId is not null && cursor.StackId != animatedStackId)
        {
            selected = request.State.Stacks.GetValueOrDefault(cursor.StackId.Value);
        }

        if (selected is not null)
        {
            MapObject? target = FindStackObject(selected.Position);

            if (target is not null)
            {
                Color color = selected.MovementPoints > 0 ? Theme.World.Highlight.SelectedColor : Theme.World.Highlight.ExhaustedColor;

                highlights.Add(new Highlight(target, color));
            }
        }

        WorldPosition? hostileTarget = cursor.HoverPath?.Encounter?.Accept(HostileEncounterVisitor.Instance, Empty.Value);

        if (hostileTarget is not null)
        {
            MapObject? target = FindFortificationObject(hostileTarget.Value) ?? FindStackObject(hostileTarget.Value);

            if (target is not null)
            {
                highlights.Add(new Highlight(target, Theme.World.Highlight.HostileColor));
            }
        }

        return new HighlightOverlay(highlights);

        MapObject? FindStackObject(WorldPosition position)
        {
            foreach (MapObject item in stacks)
            {
                if (item is Stack or CityShield && item.Position == position)
                {
                    return item;
                }
            }

            return null;
        }

        MapObject? FindFortificationObject(WorldPosition position)
        {
            WorldFortification? fortification = plan.GetFortificationAt(position);

            if (fortification is not null)
            {
                foreach (MapObject item in fortifications)
                {
                    if (item is Fortification data && data.Footprint == fortification.Footprint)
                    {
                        return data;
                    }
                }
            }

            return null;
        }
    }

    private static ReachOverlay? BuildReachOverlay(WorldRenderRequest request, WorldStackId? animatedStackId)
    {
        WorldCursor cursor = request.Cursor;

        if (cursor.StackId is not null &&
            cursor.StackId != animatedStackId &&
            cursor.Action is null &&
            cursor.ReachablePositions.Count > 0)
        {
            WorldStack? stack = request.State.Stacks.GetValueOrDefault(cursor.StackId.Value);

            if (stack is not null)
            {
                return new ReachOverlay([stack.Position, .. cursor.ReachablePositions]);
            }
        }

        return null;
    }

    private static PathOverlay? BuildSelectedPathOverlay(WorldRenderRequest request)
    {
        WorldCursor cursor = request.Cursor;
        WorldPathSelection? selection = cursor.HoverPath;

        if (selection is not null &&
            cursor.StackId is not null &&
            request.State.Stacks.TryGetValue(cursor.StackId.Value, out WorldStack? stack))
        {
            IReadOnlyList<PathStep> steps = BuildPathSteps(selection);

            if (steps.Count > 0)
            {
                return new PathOverlay(stack.Position, steps);
            }
        }

        return null;
    }

    private static IReadOnlyList<PathStep> BuildPathSteps(WorldPathSelection selection)
    {
        WorldPosition? hostileTarget = selection.Encounter?.Accept(HostileEncounterVisitor.Instance, Empty.Value);
        Color innerColor;
        Color outerColor;

        if (hostileTarget is null)
        {
            innerColor = Theme.World.Path.ValidInnerColor;
            outerColor = Theme.World.Path.ValidOuterColor;
        }
        else
        {
            innerColor = Theme.World.Path.AttackInnerColor;
            outerColor = Theme.World.Path.AttackOuterColor;
        }

        List<PathStep> steps = [];
        int totalCost = 0;

        foreach (WorldPathSegment segment in selection.Path.Segments)
        {
            totalCost += segment.Cost;
            steps.Add(new PathStep(segment.End, innerColor, outerColor, FormatCost(totalCost)));
        }

        foreach (WorldPathSegment segment in selection.Remainder.Segments)
        {
            totalCost += segment.Cost;

            if (segment.End == hostileTarget)
            {
                steps.Add(new PathStep(segment.End, Theme.World.Path.AttackInnerColor, Theme.World.Path.AttackOuterColor, FormatCost(totalCost)));
            }
            else
            {
                steps.Add(new PathStep(segment.End, Theme.World.Path.UnreachedInnerColor, Theme.World.Path.UnreachedOuterColor, FormatCost(totalCost)));
            }
        }

        return steps;
    }

    private static string FormatCost(int cost)
    {
        return cost.ToString(CultureInfo.InvariantCulture);
    }

    private static bool IsObjectVisible(WorldVisibility visibility, WorldPosition position)
    {
        return IsObjectVisible(visibility, new WorldFootprint(position, 1, 1));
    }

    private static bool IsObjectVisible(WorldVisibility visibility, WorldFootprint footprint)
    {
        WorldPosition topLeft = footprint.TopLeft;
        WorldPosition bottomRight = new(topLeft.X + footprint.Width - 1, topLeft.Y + footprint.Height - 1);
        int edgePadding = Math.Max(footprint.Width, footprint.Height) + 1;

        bool onEdge = topLeft.X < edgePadding ||
                      topLeft.Y < edgePadding ||
                      bottomRight.X >= visibility.Width - edgePadding ||
                      bottomRight.Y >= visibility.Height - edgePadding;

        if (onEdge && !visibility.IsAnyRevealed(footprint))
        {
            return false;
        }

        const int isometricPadding = 7;

        for (int y = topLeft.Y - isometricPadding; y < topLeft.Y + footprint.Height; y++)
        {
            for (int x = topLeft.X - isometricPadding; x < topLeft.X + footprint.Width; x++)
            {
                if (visibility.IsRevealed(new WorldPosition(x, y)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static int GetDepth(WorldPosition position, Layer layer)
    {
        return GetLayerDepth(GetTileDepth(position), layer);
    }

    private static int GetFootprintDepth(WorldFootprint footprint, Layer layer)
    {
        WorldPosition topLeft = footprint.TopLeft;
        WorldPosition bottomRight = new(topLeft.X + footprint.Width - 1, topLeft.Y + footprint.Height - 1);

        return GetLayerDepth((GetTileDepth(topLeft) + GetTileDepth(bottomRight)) / 2, layer);
    }

    private static int GetLayerDepth(int tileDepth, Layer layer)
    {
        int value = (int)layer;

        return value + ((tileDepth + ((value >> 9) << 8)) << 9);
    }

    private static int GetTileDepth(WorldPosition position)
    {
        return position.X + position.Y;
    }
}
