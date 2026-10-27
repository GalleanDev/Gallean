using Avalonia;
using Avalonia.Media;
using Gallean.Common;
using Gallean.Engine;
using static Gallean.Client.BattleRenderState;
using static Gallean.Client.BattleUnitState;

namespace Gallean.Client;

internal sealed class BattleRenderStateBuilder(
    BattleState baseState,
    BattleState currentState,
    IUnitStatsProvider stats,
    IGameDefinitionProvider definitions,
    IAssetMetadataProvider assets,
    AssetSpriteKey.BattleBackground background,
    BattleEnvironment environment,
    bool isAiTurn,
    BattleActionTarget? selectedTarget,
    IReadOnlyDictionary<BattleGridTile, BattleActionTarget> actionTargets,
    BattleRenderState? previousState)
{
    private enum HighlightType
    {
        None,
        Current,
        AllyTarget,
        EnemyTarget,
        SummonTarget,
        AllyReachable,
        EnemyReachable,
        SummonReachable
    }

    private enum RainType
    {
        None,
        Allowed,
        Always
    }

    private static readonly BattleFluidConfiguration FluidFogConfiguration = BattleFluidConfiguration.MakeFog();
    private static readonly BattleFluidConfiguration FluidMiasmaConfiguration = BattleFluidConfiguration.MakeMiasma();
    private static readonly BattleFluidConfiguration FluidDustConfiguration = BattleFluidConfiguration.MakeDust();
    private static readonly BattleFluidConfiguration FluidLavaConfiguration = BattleFluidConfiguration.MakeLava();
    private static readonly BattleFluidConfiguration FluidVaporConfiguration = BattleFluidConfiguration.MakeVapor();
    private static readonly BattleFluidConfiguration FluidWaterConfiguration = BattleFluidConfiguration.MakeWater();
    private static readonly BattleRainConfiguration RainConfiguration = BattleRainConfiguration.MakeDefault();
    private static readonly BattleStormConfiguration StormConfiguration = BattleStormConfiguration.MakeDefault();
    private static readonly AnimationRate HighlightRate = new(60);

    public BattleRenderState BuildState()
    {
        BattleRainConfiguration? rainConfiguration = ResolveRainConfiguration(background, environment);
        BattleLightConfiguration lightConfiguration = ResolveLightConfiguration(background, environment, rainConfiguration);
        IReadOnlyList<BattleFluidConfiguration> fluids = ResolveFluidConfiguration(background);

        List<Unit> units =
        [
            .. BuildSideUnits(baseState, BattleSide.Attacker),
            .. BuildSideUnits(baseState, BattleSide.Defender),
            .. BuildAbsentUnits()
        ];

        units.Sort((left, right) => GetDepth(left).CompareTo(GetDepth(right)));

        AnimationRate redrawRate = ComputeRedrawRate(fluids, rainConfiguration, units);

        IReadOnlyList<Placeholder> placeholders = BuildPlaceholders();
        IReadOnlyList<HitTarget> hitTargets = BuildHitTargets(units, placeholders);

        return new BattleRenderState(
            background,
            units,
            placeholders,
            hitTargets,
            lightConfiguration,
            fluids,
            rainConfiguration,
            redrawRate);
    }

    public IReadOnlyList<BattleGridUnitViewModel> BuildUnits(BattleSide side, bool isFacingRight)
    {
        UnitGrid<BattleUnit> grid = baseState.GetGrid(side);
        List<BattleGridUnitViewModel> units = [];

        foreach ((BattleUnit unit, GridPosition position) in grid.GetUnits())
        {
            bool isLarge = grid.IsLargeAt(position);
            UnitStats unitStats = stats.GetBattleUnitStats(baseState, unit.Id);
            int maxHitpoints = unitStats.MaxHitpoints;
            (int row, int column) = GetPanelCell(position, isLarge, isFacingRight);
            int columnSpan = isLarge ? 2 : 1;
            BattleGridTile tile = new(side, position);
            HighlightType highlightType = ResolveHighlightType(tile);
            Color? marker = ResolveGridMarker(highlightType);
            Presence presence = ResolvePresence(unit);
            bool isCurrent = IsUnitCurrent(unit, presence);
            UnitExperience experience = stats.GetBattleUnitExperience(baseState, unit.Id);
            IReadOnlyList<UnitModifierViewModel> modifiers = UnitModifierViewModel.CreateAll(unit, definitions);

            units.Add(new BattleGridUnitViewModel(
                unit.Id,
                tile,
                new AssetSpriteKey.UnitFace(unit.DefinitionId),
                row,
                column,
                columnSpan,
                unit.Hitpoints,
                maxHitpoints,
                presence,
                experience.IsPendingUpgrade,
                isCurrent,
                marker,
                BuildFeedback(unit, presence),
                UnitStatusViewModel.CreateAll(StatusEffect.CreateAll(unit.StatusEffects).Where(effect => effect.Type is not StatusEffectType.Cure), modifiers),
                hitpointsAnimation: AnimationRange.None));
        }

        return units;

        bool IsUnitCurrent(BattleUnit unit, Presence presence)
        {
            if (baseState.Outcome is not null)
            {
                return false;
            }
            else if (presence is Presence.Absent)
            {
                return false;
            }
            else
            {
                return unit.Id == baseState.CurrentUnitId;
            }
        }
    }

    public IReadOnlyList<BattleGridTileViewModel> BuildTiles(BattleSide side, bool isFacingRight)
    {
        List<BattleGridTileViewModel> tiles = [];

        foreach (BattleGridTile tile in GetFreeSummonTiles())
        {
            if (tile.Side == side)
            {
                (int row, int column) = GetPanelCell(tile.Position, isLarge: false, isFacingRight);
                HighlightType highlightType = ResolveHighlightType(tile);

                tiles.Add(new BattleGridTileViewModel(tile, row, column, ResolveGridMarker(highlightType)));
            }
        }

        return tiles;
    }

    private IReadOnlyList<BattleFeedbackViewModel> BuildFeedback(BattleUnit unit, Presence presence)
    {
        if (baseState.Outcome is null)
        {
            return [];
        }

        if (presence == Presence.Alive && unit.ExperienceGained > 0)
        {
            Feedback feedback = Feedback.Experience(unit.ExperienceGained);

            return [new BattleFeedbackViewModel(feedback.Type, feedback.Text, Animation: [])];
        }
        else
        {
            return [];
        }
    }

    private IReadOnlyList<Unit> BuildAbsentUnits()
    {
        HashSet<BattleUnitId> presentUnits = [];

        foreach ((BattleUnit unit, GridPosition _) in baseState.AttackerGrid.GetUnits())
        {
            presentUnits.Add(unit.Id);
        }

        foreach ((BattleUnit unit, GridPosition _) in baseState.DefenderGrid.GetUnits())
        {
            presentUnits.Add(unit.Id);
        }

        List<Unit> allUnits =
        [
            .. BuildSideUnits(currentState, BattleSide.Attacker),
            .. BuildSideUnits(currentState, BattleSide.Defender)
        ];

        List<Unit> absentUnits = [];

        foreach (Unit unit in allUnits)
        {
            if (!presentUnits.Contains(unit.Id))
            {
                absentUnits.Add(unit with { Presence = Presence.Absent, Marker = null, Highlight = null });
            }
        }

        return absentUnits;
    }

    private IReadOnlyList<Placeholder> BuildPlaceholders()
    {
        List<Placeholder> result = [];

        foreach (BattleGridTile tile in GetFreeSummonTiles())
        {
            HighlightType highlightType = ResolveHighlightType(tile);

            result.Add(new Placeholder(tile, ResolveGroundMarker(highlightType)));
        }

        return result;
    }

    private static (int Row, int Column) GetPanelCell(GridPosition position, bool isLarge, bool isFacingRight)
    {
        int row = GridPosition.FlankCount - 1 - position.Flank;

        if (isLarge)
        {
            return (row, 0);
        }
        else if (isFacingRight)
        {
            return (row, GridPosition.LineCount - 1 - position.Line);
        }
        else
        {
            return (row, position.Line);
        }
    }

    private static int GetDepth(Unit unit)
    {
        return BattleRenderDepth.GetUnitDepth(unit.Footprint);
    }

    private IReadOnlyList<BattleGridTile> GetFreeSummonTiles()
    {
        List<BattleGridTile> tiles = [];

        foreach ((BattleGridTile tile, BattleActionTarget target) in actionTargets)
        {
            if (target is BattleActionTarget.Summon)
            {
                UnitGrid<BattleUnit> grid = baseState.GetGrid(tile.Side);

                if (grid[tile.Position] is null)
                {
                    tiles.Add(tile);
                }
            }
        }

        return tiles;
    }

    private IReadOnlyList<HitTarget> BuildHitTargets(IReadOnlyList<Unit> units, IReadOnlyList<Placeholder> placeholders)
    {
        List<HitTarget> targets = [];

        foreach (Unit unit in units)
        {
            if (unit.Presence != Presence.Absent)
            {
                BattleGridTile tile = new(unit.Footprint.Side, unit.Footprint.Position);

                targets.Add(new HitTarget(tile, ResolveHitBounds(unit)));
            }
        }

        foreach (Placeholder placeholder in placeholders)
        {
            BattleGridFootprint footprint = new(placeholder.Tile.Side, placeholder.Tile.Position, IsLarge: false);

            targets.Add(new HitTarget(placeholder.Tile, BattleRenderLayout.GetPlaceholderBounds(footprint)));
        }

        return targets;
    }

    private Rect ResolveHitBounds(Unit unit)
    {
        if (unit.Presence == Presence.Dead)
        {
            return ResolveCorpseBounds(unit);
        }
        else
        {
            return ResolveBodyBounds(unit);
        }
    }

    private Rect ResolveBodyBounds(Unit unit)
    {
        AssetAnimationKey.BattleUnit bodyKey = new(unit.DefinitionId, AssetAnimationKey.BattleUnitPose.Idle, unit.Facing);
        AssetAnimationMetadata bodyMetadata = assets.GetRequiredAnimationMetadata(bodyKey);
        Rect content = new(bodyMetadata.OffsetX, bodyMetadata.OffsetY, bodyMetadata.Width, bodyMetadata.Height);

        if (unit.Auras.Get(AssetAnimationKey.BattleUnitPose.Idle) is { } auraKey)
        {
            AssetAnimationMetadata auraMetadata = assets.GetRequiredAnimationMetadata(auraKey);

            content = content.Union(new Rect(
                auraMetadata.OffsetX, auraMetadata.OffsetY,
                auraMetadata.Width, auraMetadata.Height));
        }

        return BattleRenderLayout.GetContentBounds(BattleRenderLayout.GetUnitPlacement(unit.Footprint), content);
    }

    private Rect ResolveCorpseBounds(Unit unit)
    {
        AssetSpriteKey.BattleCorpse key = new(unit.Footprint.IsLarge, unit.CorpseVariant);
        AssetSpriteMetadata metadata = assets.GetRequiredSpriteMetadata(key);
        Rect content = new(metadata.OffsetX, metadata.OffsetY, metadata.Width, metadata.Height);

        return BattleRenderLayout.GetContentBounds(BattleRenderLayout.GetCorpsePlacement(unit.Footprint), content);
    }

    private IReadOnlyList<Unit> BuildSideUnits(BattleState state, BattleSide side)
    {
        UnitGrid<BattleUnit> grid = state.GetGrid(side);
        List<Unit> result = [];

        foreach ((BattleUnit unit, GridPosition position) in grid.GetUnits())
        {
            BattleGridFootprint footprint = new(side, position, grid.IsLargeAt(position));
            UnitDefinitionId definitionId = unit.DefinitionId;
            UnitDefinition definition = definitions.GetUnit(definitionId);

            BattleGridTile tile = new(side, position);
            BattleSide facing = GetFacing(unit, side);
            Presence presence = ResolvePresence(unit);
            HighlightType highlightType = ResolveHighlightType(tile);
            Color? marker = ResolveGroundMarker(highlightType);
            Color? highlight = ResolveHighlight(highlightType);
            Auras auras = Auras.Resolve(assets, definitionId, facing);
            TimeSpan idleOffset = ResolveIdleOffset(unit.Id, footprint);
            bool frozen = unit.HasStatusEffect<BattleAbstractDisableEffect>();
            UnitStats unitStats = stats.GetBattleUnitStats(state, unit.Id);

            result.Add(new Unit(
                unit.Id,
                definitionId,
                definition.Race,
                definition.DeathAnimation,
                footprint,
                facing,
                idleOffset,
                unit.Id.Value.Mod(AssetSpriteKey.BattleCorpse.VariantCount),
                unit.Hitpoints,
                unitStats.MaxHitpoints,
                presence,
                marker,
                highlight,
                auras,
                frozen));
        }

        return result;
    }

    private TimeSpan ResolveIdleOffset(BattleUnitId unitId, BattleGridFootprint footprint)
    {
        if (previousState is not null)
        {
            foreach (Unit unit in previousState.Units)
            {
                if (unit.Id == unitId)
                {
                    return unit.IdleOffset;
                }
            }
        }

        return TimeSpan.FromTicks(StableHash.Combine((int)footprint.Side, footprint.Position.ToIndex()) % TimeSpan.TicksPerSecond);
    }

    private static Presence ResolvePresence(BattleUnit unit)
    {
        if (!unit.IsAlive)
        {
            return Presence.Dead;
        }
        else if (unit.HasStatusEffect<BattleRetreatedStatusEffect>())
        {
            return Presence.Absent;
        }
        else
        {
            return Presence.Alive;
        }
    }

    private static BattleSide GetFacing(BattleUnit unit, BattleSide side)
    {
        if (unit.HasStatusEffect<BattleRetreatStatusEffect>())
        {
            return side.GetOpposite();
        }
        else
        {
            return side;
        }
    }

    private static Color? ResolveGridMarker(HighlightType highlightType)
    {
        return highlightType switch
        {
            HighlightType.None => null,
            HighlightType.Current => null,
            HighlightType.AllyTarget => Theme.Battle.GridMarker.AllyTargetColor,
            HighlightType.EnemyTarget => Theme.Battle.GridMarker.EnemyTargetColor,
            HighlightType.SummonTarget => Theme.Battle.GridMarker.SummonTargetColor,
            HighlightType.AllyReachable => Theme.Battle.GridMarker.AllyReachableColor,
            HighlightType.EnemyReachable => Theme.Battle.GridMarker.EnemyReachableColor,
            HighlightType.SummonReachable => Theme.Battle.GridMarker.SummonReachableColor
        };
    }

    private static Color? ResolveGroundMarker(HighlightType highlightType)
    {
        return highlightType switch
        {
            HighlightType.None => null,
            HighlightType.Current => Theme.Battle.GroundMarker.CurrentColor,
            HighlightType.AllyTarget => Theme.Battle.GroundMarker.AllyTargetColor,
            HighlightType.EnemyTarget => Theme.Battle.GroundMarker.EnemyTargetColor,
            HighlightType.SummonTarget => Theme.Battle.GroundMarker.SummonTargetColor,
            HighlightType.AllyReachable => Theme.Battle.GroundMarker.AllyReachableColor,
            HighlightType.EnemyReachable => null,
            HighlightType.SummonReachable => Theme.Battle.GroundMarker.SummonReachableColor
        };
    }

    private static Color? ResolveHighlight(HighlightType highlightType)
    {
        return highlightType switch
        {
            HighlightType.None => null,
            HighlightType.Current => Theme.Battle.Highlight.CurrentColor,
            HighlightType.AllyTarget => Theme.Battle.Highlight.AllyTargetColor,
            HighlightType.EnemyTarget => Theme.Battle.Highlight.EnemyTargetColor,
            HighlightType.SummonTarget => Theme.Battle.Highlight.SummonTargetColor,
            HighlightType.AllyReachable => Theme.Battle.Highlight.AllyReachableColor,
            HighlightType.EnemyReachable => null,
            HighlightType.SummonReachable => Theme.Battle.Highlight.SummonReachableColor
        };
    }

    private bool IsCurrent(BattleGridTile tile)
    {
        UnitGrid<BattleUnit> grid = baseState.GetGrid(tile.Side);
        BattleUnit? tileUnit = grid[tile.Position];

        return baseState.Outcome is null &&
               !isAiTurn &&
               tileUnit is not null &&
               tileUnit.Id == baseState.CurrentUnitId &&
               ResolvePresence(tileUnit) == Presence.Alive;
    }

    private HighlightType ResolveHighlightType(BattleGridTile tile)
    {
        if (baseState.Outcome is not null)
        {
            return HighlightType.None;
        }

        UnitGrid<BattleUnit> grid = baseState.GetGrid(tile.Side);
        BattleUnit? tileUnit = grid[tile.Position];
        BattleUnit currentUnit = baseState.GetUnit(baseState.CurrentUnitId);
        bool isAlly = tile.Side == currentUnit.Side;
        bool isTarget = tileUnit is not null && selectedTarget is BattleActionTarget.Unit unit && unit.Units.Contains(tileUnit.Id);
        bool isTargetSummon = selectedTarget is BattleActionTarget.Summon summon && summon.Tiles.Contains(tile);

        if (isTarget)
        {
            return isAlly ? HighlightType.AllyTarget : HighlightType.EnemyTarget;
        }
        else if (IsCurrent(tile))
        {
            return HighlightType.Current;
        }
        else
        {
            if (actionTargets.TryGetValue(tile, out BattleActionTarget? actionTarget))
            {
                if (actionTarget is BattleActionTarget.Summon)
                {
                    return isTargetSummon ? HighlightType.SummonTarget : HighlightType.SummonReachable;
                }
                else if (isAlly)
                {
                    return HighlightType.AllyReachable;
                }
                else
                {
                    return HighlightType.EnemyReachable;
                }
            }
            else
            {
                return HighlightType.None;
            }
        }
    }

    private static AnimationRate ComputeRedrawRate(
        IReadOnlyList<BattleFluidConfiguration> fluids,
        BattleRainConfiguration? rainConfiguration,
        IReadOnlyList<Unit> units)
    {
        AnimationRate rate = AnimationRate.Sprites;

        foreach (BattleFluidConfiguration fluid in fluids)
        {
            if (fluid.Rate.FramesPerSecond > rate.FramesPerSecond)
            {
                rate = fluid.Rate;
            }
        }

        if (rainConfiguration is not null && rainConfiguration.Rate.FramesPerSecond > rate.FramesPerSecond)
        {
            rate = rainConfiguration.Rate;
        }

        foreach (Unit unit in units)
        {
            if (unit.Highlight is not null && HighlightRate.FramesPerSecond > rate.FramesPerSecond)
            {
                rate = HighlightRate;
            }
        }

        return rate;
    }

    private static BattleLightConfiguration ResolveLightConfiguration(
        AssetSpriteKey.BattleBackground background,
        BattleEnvironment environment,
        BattleRainConfiguration? rainConfiguration)
    {
        IReadOnlyList<BattleLight> lights = ResolveBackgroundLights(background);

        BattleLightConfiguration configuration = environment.TimeOfDay switch
        {
            BattleTimeOfDay.Dawn => BattleLightConfiguration.MakeDawn(lights),
            BattleTimeOfDay.Midday => BattleLightConfiguration.MakeMidday(lights),
            BattleTimeOfDay.Evening => BattleLightConfiguration.MakeEvening(lights),
            BattleTimeOfDay.Dusk => BattleLightConfiguration.MakeDusk(lights)
        };

        return configuration with { Storm = ResolveStormConfiguration(environment, rainConfiguration) };
    }

    private static IReadOnlyList<BattleLight> ResolveBackgroundLights(AssetSpriteKey.BattleBackground background)
    {
        return (background.Location, background.Variant) switch
        {
            (AssetSpriteKey.BattleLocation.Terrain { Value: WorldMapTerrain.Heretic }, 0) =>
            [
                BattleLight.MakeLava(new Point(982, 2), range: 174, power: 2.59),
                BattleLight.MakeLava(new Point(966, 55), range: 90, power: 2.49)
            ],
            (AssetSpriteKey.BattleLocation.Terrain { Value: WorldMapTerrain.Heretic }, 1) =>
            [
                BattleLight.MakeLava(new Point(83, 459), range: 260, power: 3.00),
                BattleLight.MakeLava(new Point(1101, 100), range: 84, power: 1.94),
                BattleLight.MakeLava(new Point(396, 98), range: 171, power: 2.75),
                BattleLight.MakeLava(new Point(801, 520), range: 260, power: 2.68)
            ],
            (AssetSpriteKey.BattleLocation.Terrain, _) => [],
            (AssetSpriteKey.BattleLocation.City, 3) =>
            [
                BattleLight.MakeLantern(new Point(-136, 58), range: 60, power: 2.03),
                BattleLight.MakeLantern(new Point(1089, 123), range: 122, power: 1.90),
                BattleLight.MakeLantern(new Point(429, 91), range: 173, power: 2.05),
                BattleLight.MakeLantern(new Point(617, 79), range: 178, power: 1.99)
            ],
            (AssetSpriteKey.BattleLocation.City, _) => [],
            (AssetSpriteKey.BattleLocation.Ruin, _) => [],
            (AssetSpriteKey.BattleLocation.Capital { Race: WorldRace.Human }, _) =>
            [
                BattleLight.MakeWindow(new Point(397, 2), range: 54, power: 1.48),
                BattleLight.MakeWindow(new Point(736, 10), range: 176, power: 1.87)
            ],
            (AssetSpriteKey.BattleLocation.Capital { Race: WorldRace.Undead }, _) =>
            [
                BattleLight.MakeGhostCrystal(new Point(691, 44), range: 54, power: 1.53),
                BattleLight.MakeGhostCrystal(new Point(863, 42), range: 54, power: 1.48),
                BattleLight.MakeGhostCrystal(new Point(812, 17), range: 126, power: 1.42)
            ],
            (AssetSpriteKey.BattleLocation.Capital { Race: WorldRace.Dwarf }, _) =>
            [
                BattleLight.MakeLava(new Point(166, 75), range: 211, power: 2.21),
                BattleLight.MakeLava(new Point(1069, 68), range: 130, power: 2.02)
            ],
            (AssetSpriteKey.BattleLocation.Capital { Race: WorldRace.Heretic }, _) =>
            [
                BattleLight.MakeLava(new Point(470, 91), range: 247, power: 3.00),
                BattleLight.MakeLava(new Point(-67, 89), range: 200, power: 3.00),
                BattleLight.MakeLava(new Point(1067, 91), range: 91, power: 2.41),
                BattleLight.MakeLava(new Point(179, 85), range: 169, power: 3.00)
            ],
            (AssetSpriteKey.BattleLocation.Capital { Race: WorldRace.Elf }, _) =>
            [
                BattleLight.MakeGlowingWater(new Point(222, 171), range: 102, power: 2.26),
                BattleLight.MakeGlowingWater(new Point(849, 174), range: 181, power: 3.00),
                BattleLight.MakeGlowingWater(new Point(69, 164), range: 120, power: 2.15),
                BattleLight.MakeGlowingWater(new Point(657, 167), range: 78, power: 2.04)
            ],
            (AssetSpriteKey.BattleLocation.Capital, _) => [],
            (AssetSpriteKey.BattleLocation.Boat, _) => [],
            _ => throw new ArgumentOutOfRangeException(nameof(background), background, null)
        };
    }

    private static IReadOnlyList<BattleFluidConfiguration> ResolveFluidConfiguration(AssetSpriteKey.BattleBackground background)
    {
        return (background.Location, background.Variant) switch
        {
            (AssetSpriteKey.BattleLocation.Terrain { Value: WorldMapTerrain.Human }, 1) => [FluidFogConfiguration],
            (AssetSpriteKey.BattleLocation.Terrain { Value: WorldMapTerrain.Neutral }, 0 or 2) => [FluidFogConfiguration],
            (AssetSpriteKey.BattleLocation.Terrain { Value: WorldMapTerrain.Neutral }, 1) => [FluidDustConfiguration],
            (AssetSpriteKey.BattleLocation.Terrain { Value: WorldMapTerrain.Undead }, _) => [FluidFogConfiguration],
            (AssetSpriteKey.BattleLocation.Terrain { Value: WorldMapTerrain.Heretic }, _) => [FluidLavaConfiguration, FluidVaporConfiguration],
            (AssetSpriteKey.BattleLocation.Terrain { Value: WorldMapTerrain.Elf }, _) => [FluidFogConfiguration],
            (AssetSpriteKey.BattleLocation.Terrain { Value: WorldMapTerrain.Water }, 0) => [FluidFogConfiguration],
            (AssetSpriteKey.BattleLocation.Terrain, _) => [],
            (AssetSpriteKey.BattleLocation.City, 1 or 2 or 3) => [FluidFogConfiguration],
            (AssetSpriteKey.BattleLocation.City, _) => [],
            (AssetSpriteKey.BattleLocation.Ruin, 1) => [FluidMiasmaConfiguration],
            (AssetSpriteKey.BattleLocation.Ruin, _) => [FluidFogConfiguration],
            (AssetSpriteKey.BattleLocation.Capital { Race: WorldRace.Undead }, _) => [FluidMiasmaConfiguration],
            (AssetSpriteKey.BattleLocation.Capital { Race: WorldRace.Heretic or WorldRace.Dwarf }, _) => [FluidLavaConfiguration, FluidVaporConfiguration],
            (AssetSpriteKey.BattleLocation.Capital { Race: WorldRace.Elf }, _) => [FluidWaterConfiguration],
            (AssetSpriteKey.BattleLocation.Capital, _) => [],
            (AssetSpriteKey.BattleLocation.Boat, 0 or 1) => [FluidFogConfiguration],
            (AssetSpriteKey.BattleLocation.Boat, _) => [],
            _ => throw new ArgumentOutOfRangeException(nameof(background), background, null)
        };
    }

    private static RainType ResolveRainType(AssetSpriteKey.BattleBackground background)
    {
        return (background.Location, background.Variant) switch
        {
            (AssetSpriteKey.BattleLocation.Terrain { Value: WorldMapTerrain.Human }, 0 or 1) => RainType.Allowed,
            (AssetSpriteKey.BattleLocation.Terrain { Value: WorldMapTerrain.Undead }, 0 or 1) => RainType.Allowed,
            (AssetSpriteKey.BattleLocation.Terrain { Value: WorldMapTerrain.Heretic }, 0) => RainType.Allowed,
            (AssetSpriteKey.BattleLocation.Terrain { Value: WorldMapTerrain.Neutral }, 0 or 1 or 2 or 3) => RainType.Allowed,
            (AssetSpriteKey.BattleLocation.Terrain { Value: WorldMapTerrain.Elf }, 0 or 1) => RainType.Allowed,
            (AssetSpriteKey.BattleLocation.Terrain { Value: WorldMapTerrain.Water }, 0) => RainType.Allowed,
            (AssetSpriteKey.BattleLocation.Terrain, _) => RainType.None,
            (AssetSpriteKey.BattleLocation.City, 0 or 1 or 2) => RainType.Allowed,
            (AssetSpriteKey.BattleLocation.City, 3) => RainType.Always,
            (AssetSpriteKey.BattleLocation.City, _) => RainType.None,
            (AssetSpriteKey.BattleLocation.Ruin, 0 or 1 or 2) => RainType.Allowed,
            (AssetSpriteKey.BattleLocation.Ruin, _) => RainType.None,
            (AssetSpriteKey.BattleLocation.Capital, _) => RainType.None,
            (AssetSpriteKey.BattleLocation.Boat, 0) => RainType.Always,
            (AssetSpriteKey.BattleLocation.Boat, _) => RainType.None,
            _ => throw new ArgumentOutOfRangeException(nameof(background), background, null)
        };
    }

    private static BattleRainConfiguration? ResolveRainConfiguration(AssetSpriteKey.BattleBackground background, BattleEnvironment environment)
    {
        RainType rainType = ResolveRainType(background);

        return rainType switch
        {
            RainType.None => null,
            RainType.Allowed when environment.Rain => RainConfiguration,
            RainType.Allowed => null,
            RainType.Always => RainConfiguration
        };
    }

    private static BattleStormConfiguration? ResolveStormConfiguration(BattleEnvironment environment, BattleRainConfiguration? rainConfiguration)
    {
        if (rainConfiguration is not null && environment.Storm)
        {
            return StormConfiguration;
        }
        else
        {
            return null;
        }
    }
}
