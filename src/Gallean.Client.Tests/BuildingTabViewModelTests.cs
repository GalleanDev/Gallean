using System.Collections.Immutable;
using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class BuildingTabViewModelTests
{
    private static WorldPlayerBuildingDefinition BuildingDefinition(
        WorldPlayerBuildingDefinitionId id,
        string name,
        WorldPlayerBuildingDefinitionId? requiredId,
        UnitBranch? unitBranch,
        int level,
        WorldResourceSet cost,
        ImmutableArray<UnitDefinitionId> unitIds)
    {
        return new WorldPlayerBuildingDefinition(
            Id: id,
            NameTextId: new GlobalTextId(name),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: requiredId,
            Category: WorldPlayerBuildingCategory.Unit,
            UnitBranch: unitBranch,
            Level: level,
            Cost: cost,
            UnitIds: unitIds);
    }

    private static IReadOnlyList<string> FormatGraph(BuildingTabViewModel screen)
    {
        BuildingGraphViewModel graph = screen.Graph;
        List<string> rows = [$"hub {graph.HubRadiusX:F2}x{graph.HubRadiusY:F2} size {graph.HubSize:F2} projection {graph.ProjectionY:F2} | extent {graph.ExtentX:F2}x{graph.ExtentY:F2}"];

        foreach (BuildingBranchViewModel branch in graph.Branches)
        {
            rows.Add($"legend {branch.Type}: {branch.Title} {branch.Icon} | fork ({branch.ForkX:F2}, {branch.ForkY:F2})");
        }

        foreach (BuildingRingViewModel ring in graph.Rings)
        {
            rows.Add($"ring {ring.Title} {ring.Radius:F2}");
        }

        foreach (BuildingNodeViewModel node in graph.Nodes)
        {
            rows.Add($"{FormatSelection(node.IsSelected)}{FormatBranch(node)}{FormatRoot(node)} ({node.X:F2}, {node.Y:F2}): {FormatNode(node)} | {WorldResourceFormatter.Format(node.Cost)}");
        }

        foreach (BuildingLink link in graph.Links)
        {
            rows.Add($"{link.From.Name} -> {link.To.Name}");
        }

        return rows;
    }

    private static string FormatBranch(BuildingNodeViewModel node)
    {
        return node.Branch is null ? "Common" : node.Branch.Type.ToString();
    }

    private static string FormatRoot(BuildingNodeViewModel node)
    {
        return node.IsBranchRoot ? " root" : string.Empty;
    }

    private static IReadOnlyList<string> FormatDetails(BuildingTabViewModel screen)
    {
        BuildingNodeViewModel selected = Assert.NotNull(screen.SelectedBuilding);
        List<string> rows = [$"{FormatNode(selected)} | {selected.LevelText} | {WorldResourceFormatter.Format(selected.Cost)} | {FormatOffer(selected.Offer)} | {selected.PortraitKey}"];

        if (selected.LockedText is not null)
        {
            rows.Add(selected.LockedText.Text);
        }

        JoinStringBuilder selectionLocked = new(", ");

        foreach (BuildingNodeViewModel node in screen.Graph.Nodes)
        {
            if (node.IsSelectionLocked)
            {
                selectionLocked.Add(node.Name);
            }
        }

        if (selectionLocked.Count > 0)
        {
            rows.Add($"selection-locked: {selectionLocked}");
        }

        IDetailsSource detailsSource = selected;

        switch (detailsSource.DetailsContent)
        {
            case UnitDetailsViewModel unit:
                rows.Add($"details: {unit.Name}");
                break;
            case BuildingDetailsViewModel building:
                rows.Add($"details: {building.Name} | {building.IconKey} | {building.Description}");
                break;
        }

        rows.Add(screen.ConstructText);

        return rows;
    }

    private static string FormatOffer(OfferStatusViewModel offer)
    {
        if (offer.UnavailableReason is not null)
        {
            return $"reason: {offer.UnavailableReason}";
        }
        else if (Assert.NotNull(offer.Price).IsShort)
        {
            return "short";
        }
        else
        {
            return "affordable";
        }
    }

    private static string FormatNode(BuildingNodeViewModel node)
    {
        JoinStringBuilder units = new(", ");

        foreach (BuildingUnitViewModel unit in node.Units)
        {
            units.Add(unit.Name);
        }

        return $"{node.Name} | {FormatStatus(node)} | {units}";
    }

    private static string FormatStatus(BuildingNodeViewModel node)
    {
        return node.Status switch
        {
            BuildingStatus.Constructible => "constructible",
            BuildingStatus.Built => "built",
            BuildingStatus.BranchLocked => $"branch-locked: {node.StatusText}",
            BuildingStatus.MissingPrerequisite => $"missing: {node.StatusText}",
            BuildingStatus.ScenarioLocked => $"scenario-locked: {node.StatusText}",
            BuildingStatus.Unavailable => $"unavailable: {node.StatusText}",
            _ => throw new ArgumentOutOfRangeException(nameof(node), node.Status, null)
        };
    }

    private static string FormatSelection(bool isSelected)
    {
        return isSelected ? "*" : string.Empty;
    }

    [Test]
    public void TreeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldResourceSet cost = new(100, 0, 0, 0, 0, 0);
        WorldPlayerBuildingDefinitionId stableId = new("bb01");
        WorldPlayerBuildingDefinitionId imperialStableId = new("bb02");
        WorldPlayerBuildingDefinitionId shrineId = new("bb03");
        WorldPlayerBuildingDefinitionId holyStatueId = new("bb04");
        WorldPlayerBuildingDefinitionId chapelId = new("bb05");
        WorldPlayerBuildingDefinitionId fountainId = new("bb06");
        WorldPlayerBuildingDefinitionId dungeonId = new("bb07");
        WorldPlayerBuildingDefinitionId tortureChamberId = new("bb08");
        WorldPlayerBuildingDefinitionId magistrateId = new("bb09");
        WorldPlayerBuildingDefinitionId libraryId = new("bb12");
        WorldPlayerBuildingDefinitionId ruinedTempleId = new("bb22");
        WorldPlayerBuildingDefinitionId templeId = new("bb23");
        WorldPlayerBuildingDefinitionId citadelId = new("bb30");

        UnitDefinition paladin = TestHelpers.SimpleUnitDefinition("Paladin", maxHitpoints: 100, initiative: 50, damage: 35) with
        {
            NameTextId = new GlobalTextId("Paladin")
        };

        UnitDefinition holyAvenger = TestHelpers.SimpleUnitDefinition("HolyAvenger", maxHitpoints: 150, initiative: 50, damage: 50) with
        {
            NameTextId = new GlobalTextId("Holy Avenger")
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(5000, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings(
                [
                    stableId,
                    imperialStableId,
                    shrineId,
                    holyStatueId,
                    chapelId,
                    fountainId,
                    dungeonId,
                    tortureChamberId,
                    magistrateId,
                    libraryId,
                    ruinedTempleId,
                    templeId
                ],
                [stableId, imperialStableId])
        };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior) with
            {
                BuildingIds = [stableId, imperialStableId, shrineId, holyStatueId, chapelId, fountainId, dungeonId, tortureChamberId, magistrateId, libraryId, ruinedTempleId, templeId, citadelId]
            })
            .WithPlayer(player)
            .WithDefinition(BuildingDefinition(citadelId, "Citadel", holyStatueId, UnitBranch.Fighter, 6, cost, []))
            .WithDefinition(BuildingDefinition(stableId, "Stable", null, UnitBranch.Fighter, 2, cost, []))
            .WithDefinition(BuildingDefinition(imperialStableId, "Imperial Stable", stableId, UnitBranch.Fighter, 3, cost, []))
            .WithDefinition(BuildingDefinition(shrineId, "Shrine", imperialStableId, UnitBranch.Fighter, 4, cost, [paladin.Id]))
            .WithDefinition(BuildingDefinition(holyStatueId, "Holy Statue", shrineId, UnitBranch.Fighter, 5, cost, [holyAvenger.Id]))
            .WithDefinition(BuildingDefinition(chapelId, "Chapel", shrineId, UnitBranch.Fighter, 5, cost, []))
            .WithDefinition(BuildingDefinition(fountainId, "Fountain of Life", imperialStableId, UnitBranch.Fighter, 4, cost, []))
            .WithDefinition(BuildingDefinition(dungeonId, "Dungeon", null, UnitBranch.Fighter, 2, cost, []))
            .WithDefinition(BuildingDefinition(tortureChamberId, "Torture Chamber", dungeonId, UnitBranch.Fighter, 3, cost, []))
            .WithDefinition(BuildingDefinition(magistrateId, "Magistrate", tortureChamberId, UnitBranch.Fighter, 4, cost, []))
            .WithDefinition(BuildingDefinition(libraryId, "Library", null, UnitBranch.Mage, 2, cost, []))
            .WithDefinition(BuildingDefinition(ruinedTempleId, "Ruined Temple", libraryId, UnitBranch.Sideshow, 3, cost, []))
            .WithDefinition(BuildingDefinition(templeId, "Temple", null, null, 2, cost, []))
            .WithDefinition(paladin)
            .WithDefinition(holyAvenger)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, ResourceSetProjection.Gold, owner => [new BuildingTabViewModel(gameModel, gameViewModel, owner)]);
        BuildingTabViewModel screen = Assert.IsOfType<BuildingTabViewModel>(management.Tabs[0]);

        Assert.That(
            FormatGraph(screen),
            Is.EqualTo([
                "hub 1.70x1.90 size 0.00 projection 0.80 | extent 9.00x7.32",
                "legend Fighter: Fighters Damage | fork (-1.87, -1.66)",
                "legend Mage: Mages Spellbook | fork (-1.87, 1.66)",
                "ring Level 2 3.40",
                "ring Level 3 4.65",
                "ring Level 4 5.90",
                "ring Level 5 7.15",
                "ring Level 6 8.40",
                "Common (0.00, 0.00): Temple | constructible |  | 100 gold",
                "Fighter root (-2.66, -1.69): Stable | built |  | 100 gold",
                "Fighter (-3.64, -2.32): Imperial Stable | built |  | 100 gold",
                "Fighter (-5.16, -2.29): Shrine | constructible | Paladin | 100 gold",
                "Fighter (-6.74, -1.91): Holy Statue | missing: Requires Shrine | Holy Avenger | 100 gold",
                "Fighter (-7.92, -2.24): Citadel | scenario-locked: Not available in this quest |  | 100 gold",
                "Fighter (-5.60, -3.56): Chapel | missing: Requires Shrine |  | 100 gold",
                "Fighter (-3.17, -3.98): Fountain of Life | constructible |  | 100 gold",
                "Fighter root (-0.79, -2.64): Dungeon | branch-locked: Branch closed |  | 100 gold",
                "Fighter (-1.09, -3.62): Torture Chamber | branch-locked: Branch closed |  | 100 gold",
                "Fighter (-1.38, -4.59): Magistrate | branch-locked: Branch closed |  | 100 gold",
                "Mage root (-2.28, 2.02): Library | constructible |  | 100 gold",
                "Mage (-3.11, 2.76): Ruined Temple | missing: Requires Library |  | 100 gold",
                "Stable -> Imperial Stable",
                "Imperial Stable -> Shrine",
                "Shrine -> Holy Statue",
                "Holy Statue -> Citadel",
                "Shrine -> Chapel",
                "Imperial Stable -> Fountain of Life",
                "Dungeon -> Torture Chamber",
                "Torture Chamber -> Magistrate",
                "Library -> Ruined Temple"
            ]));

        Assert.That(screen.SelectedBuilding, Is.Null);
        Assert.That(screen.ConstructCommand.CanExecute(null), Is.False);

        screen.SelectBuildingCommand.Execute(screen.Graph.Nodes[3]);

        Assert.That(
            FormatDetails(screen),
            Is.EqualTo([
                "Shrine | constructible | Paladin | Level 4 | 100 gold | affordable | unit-face/paladin",
                "Permanently locks out Fountain of Life",
                "selection-locked: Fountain of Life",
                "details: Paladin",
                "Build Shrine"
            ]));

        Assert.That(
            screen.Graph.Nodes[3].LockedText,
            Is.EqualTo(new TextSpans([
                new TextSpan("Permanently locks out ", TextStyle.None),
                new TextSpan("Fountain of Life", TextStyle.Italic)
            ])));
        Assert.That(screen.Graph.Nodes[3].IsSelected, Is.True);
        Assert.That(screen.ConstructCommand.CanExecute(null), Is.True);

        screen.SelectBuildingCommand.Execute(screen.Graph.Nodes[11]);

        Assert.That(
            FormatDetails(screen),
            Is.EqualTo([
                "Library | constructible |  | Level 2 | 100 gold | affordable | building-icon/bb12",
                "details: Library | building-icon/bb12 | description",
                "Build Library"
            ]));

        Assert.That(screen.Graph.Nodes[3].IsSelected, Is.False);
        Assert.That(screen.Graph.Nodes[11].IsSelected, Is.True);

        screen.SelectBuildingCommand.Execute(screen.Graph.Nodes[12]);

        Assert.That(
            FormatDetails(screen),
            Is.EqualTo([
                "Ruined Temple | missing: Requires Library |  | Level 3 | 100 gold | reason: Requires Library | building-icon/bb22",
                "details: Ruined Temple | building-icon/bb22 | description",
                "Build Ruined Temple"
            ]));

        Assert.That(screen.ConstructCommand.CanExecute(null), Is.False);

        screen.SelectBuildingCommand.Execute(screen.Graph.Nodes[9]);

        Assert.That(
            FormatDetails(screen),
            Is.EqualTo([
                "Torture Chamber | branch-locked: Branch closed |  | Level 3 | 100 gold | reason: Branch closed | building-icon/bb08",
                "details: Torture Chamber | building-icon/bb08 | description",
                "Build Torture Chamber"
            ]));

        screen.SelectBuildingCommand.Execute(screen.Graph.Nodes[5]);

        Assert.That(
            FormatDetails(screen),
            Is.EqualTo([
                "Citadel | scenario-locked: Not available in this quest |  | Level 6 | 100 gold | reason: Not available in this quest | building-icon/bb30",
                "details: Citadel | building-icon/bb30 | description",
                "Build Citadel"
            ]));

        Assert.That(screen.ConstructCommand.CanExecute(null), Is.False);

        screen.ClearSelectionCommand.Execute(null);

        Assert.That(screen.SelectedBuilding, Is.Null);
        Assert.That(screen.Graph.Nodes[9].IsSelected, Is.False);
    }

    [Test]
    public async Task ConstructBuildingTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldResourceSet cost = new(100, 0, 0, 0, 0, 0);
        WorldPlayerBuildingDefinitionId barracksId = new("Barracks");
        WorldPlayerBuildingDefinitionId advancedBarracksId = new("AdvancedBarracks");
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([barracksId, advancedBarracksId, mageGuildId], [])
        };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(BuildingDefinition(barracksId, "Barracks", null, UnitBranch.Fighter, 1, cost, []))
            .WithDefinition(BuildingDefinition(advancedBarracksId, "Advanced barracks", barracksId, UnitBranch.Fighter, 2, cost, []))
            .WithDefinition(BuildingDefinition(mageGuildId, "Mage guild", null, null, 1, cost, []))
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, ResourceSetProjection.Gold, owner => [new BuildingTabViewModel(gameModel, gameViewModel, owner)]);
        BuildingTabViewModel screen = Assert.IsOfType<BuildingTabViewModel>(management.Tabs[0]);

        Assert.That(
            FormatGraph(screen),
            Is.EqualTo([
                "hub 1.70x1.90 size 0.00 projection 0.80 | extent 5.25x4.32",
                "legend Fighter: Fighters Damage | fork (-1.87, -1.66)",
                "ring Level 1 3.40",
                "ring Level 2 4.65",
                "Common (0.00, 0.00): Mage guild | constructible |  | 100 gold",
                "Fighter root (-2.28, -2.02): Barracks | constructible |  | 100 gold",
                "Fighter (-3.11, -2.76): Advanced barracks | missing: Requires Barracks |  | 100 gold",
                "Barracks -> Advanced barracks"
            ]));

        screen.SelectBuildingCommand.Execute(screen.Graph.Nodes[1]);

        await screen.ConstructCommand.ExecuteAsync(null);

        Assert.That(session.World.State.Players[player.Id].Buildings.Acquired, Is.EqualTo([barracksId]));
        Assert.That(screen.Game.Alert.Current, Is.EqualTo(new Alert(AlertType.Info, IconMoniker.Capital, "Building constructed", "Barracks")));
        Assert.That(screen.SelectedBuilding, Is.Null);

        Assert.That(
            FormatGraph(screen),
            Is.EqualTo([
                "hub 1.70x1.90 size 0.00 projection 0.80 | extent 5.25x4.32",
                "legend Fighter: Fighters Damage | fork (-1.87, -1.66)",
                "ring Level 1 3.40",
                "ring Level 2 4.65",
                "Common (0.00, 0.00): Mage guild | unavailable: You have already constructed a building this turn |  | 100 gold",
                "Fighter root (-2.28, -2.02): Barracks | built |  | 100 gold",
                "Fighter (-3.11, -2.76): Advanced barracks | unavailable: You have already constructed a building this turn |  | 100 gold",
                "Barracks -> Advanced barracks"
            ]));

        Assert.That(screen.ConstructCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void InsufficientResourcesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Buildings = new WorldPlayerBuildings([mageGuildId], [])
        };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(BuildingDefinition(mageGuildId, "Mage guild", null, null, 1, new WorldResourceSet(200, 0, 0, 0, 0, 0), []))
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, ResourceSetProjection.Gold, owner => [new BuildingTabViewModel(gameModel, gameViewModel, owner)]);
        BuildingTabViewModel screen = Assert.IsOfType<BuildingTabViewModel>(management.Tabs[0]);

        Assert.That(
            FormatGraph(screen),
            Is.EqualTo([
                "hub 1.70x1.90 size 0.00 projection 0.80 | extent 2.30x2.50",
                "Common (0.00, 0.00): Mage guild | unavailable: Not enough resources |  | 200 gold"
            ]));

        screen.SelectBuildingCommand.Execute(screen.Graph.Nodes[0]);

        Assert.That(
            FormatDetails(screen),
            Is.EqualTo([
                "Mage guild | unavailable: Not enough resources |  | Building | 200 gold | reason: Not enough resources | building-icon/mageguild",
                "details: Mage guild | building-icon/mageguild | description",
                "Build Mage guild"
            ]));

        Assert.That(screen.ConstructCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task CapitalConstructBuildingTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);
        WorldResourceSet cost = new(100, 0, 0, 0, 0, 0);
        WorldPlayerBuildingDefinitionId towerId = new("G000BB0012");
        WorldPlayerBuildingDefinitionId hallId = new("G000BB0023");

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([towerId, hallId], [hallId])
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 35, 0, soldierDefinition.Level, [], null, false);

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Human,
            NameTextId: new GlobalTextId("Human"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Human,
            IsPlayable: true,
            CapitalRegeneration: 35,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [soldierDefinition.Id],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(soldier, GridPosition.FrontCenter, false)
                .Build());

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(BuildingDefinition(towerId, "Tower", null, null, 1, cost, []))
            .WithDefinition(BuildingDefinition(hallId, "Hall", null, null, 1, cost, []))
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithRace(raceDefinition)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(
            gameModel,
            gameViewModel,
            ScreenMusic.Keep,
            null,
            ResourceSetProjection.Gold,
            owner =>
            [
                new CityTabViewModel(gameModel, gameViewModel, owner, capital.Id),
                new BuildingTabViewModel(gameModel, gameViewModel, owner)
            ]);
        CityTabViewModel city = Assert.IsOfType<CityTabViewModel>(management.Tabs[0]);
        BuildingTabViewModel screen = Assert.IsOfType<BuildingTabViewModel>(management.Tabs[1]);

        management.SelectTabCommand.Execute(screen);

        Assert.That(
            FormatGraph(screen),
            Is.EqualTo([
                "hub 1.90x1.90 size 0.00 projection 0.80 | extent 2.50x2.50",
                "Common (-0.80, 0.00): Tower | constructible |  | 100 gold",
                "Common (0.80, 0.00): Hall | built |  | 100 gold"
            ]));

        screen.SelectBuildingCommand.Execute(screen.Graph.Nodes[0]);

        await screen.ConstructCommand.ExecuteAsync(null);

        Assert.That(management.SelectedTab, Is.SameAs(screen));
        Assert.That(screen.Game.Alert.Current, Is.EqualTo(new Alert(AlertType.Info, IconMoniker.Capital, "Building constructed", "Tower")));
        Assert.That(screen.PanelsVisible, Is.False);

        CapitalSceneViewModel scene = Assert.NotNull(screen.CapitalScene);

        Assert.That(scene.Layers, Is.EqualTo([
            new CapitalSceneLayerViewModel(
                new AssetSpriteKey.CapitalBackground(WorldRace.Human, 0),
                AnimationKey: null,
                IsConstructed: false),
            new CapitalSceneLayerViewModel(
                new AssetSpriteKey.CapitalBackground(WorldRace.Human, 1),
                AnimationKey: null,
                IsConstructed: false),
            new CapitalSceneLayerViewModel(
                SpriteKey: null,
                AnimationKey: new AssetAnimationKey.CapitalBackgroundOverlay(WorldRace.Human, 0),
                IsConstructed: false),
            new CapitalSceneLayerViewModel(
                SpriteKey: null,
                AnimationKey: new AssetAnimationKey.CapitalBuilding(towerId),
                IsConstructed: true),
            new CapitalSceneLayerViewModel(
                new AssetSpriteKey.CapitalBackground(WorldRace.Human, 2),
                AnimationKey: null,
                IsConstructed: false),
            new CapitalSceneLayerViewModel(
                SpriteKey: null,
                AnimationKey: new AssetAnimationKey.CapitalBackgroundOverlay(WorldRace.Human, 1),
                IsConstructed: false),
            new CapitalSceneLayerViewModel(
                SpriteKey: null,
                AnimationKey: new AssetAnimationKey.CapitalBuilding(hallId),
                IsConstructed: false)
        ]));

        CapitalSceneViewModel cityScene = Assert.NotNull(city.CapitalScene);

        Assert.That(cityScene.Layers, Is.EqualTo([
            new CapitalSceneLayerViewModel(
                new AssetSpriteKey.CapitalBackground(WorldRace.Human, 0),
                AnimationKey: null,
                IsConstructed: false),
            new CapitalSceneLayerViewModel(
                new AssetSpriteKey.CapitalBackground(WorldRace.Human, 1),
                AnimationKey: null,
                IsConstructed: false),
            new CapitalSceneLayerViewModel(
                SpriteKey: null,
                AnimationKey: new AssetAnimationKey.CapitalBackgroundOverlay(WorldRace.Human, 0),
                IsConstructed: false),
            new CapitalSceneLayerViewModel(
                SpriteKey: null,
                AnimationKey: new AssetAnimationKey.CapitalBuilding(towerId),
                IsConstructed: false),
            new CapitalSceneLayerViewModel(
                new AssetSpriteKey.CapitalBackground(WorldRace.Human, 2),
                AnimationKey: null,
                IsConstructed: false),
            new CapitalSceneLayerViewModel(
                SpriteKey: null,
                AnimationKey: new AssetAnimationKey.CapitalBackgroundOverlay(WorldRace.Human, 1),
                IsConstructed: false),
            new CapitalSceneLayerViewModel(
                SpriteKey: null,
                AnimationKey: new AssetAnimationKey.CapitalBuilding(hallId),
                IsConstructed: false)
        ]));

        screen.ShowPanelsCommand.Execute(null);

        Assert.That(screen.PanelsVisible, Is.True);

        management.SelectTabCommand.Execute(city);

        CapitalSceneViewModel settledScene = Assert.NotNull(screen.CapitalScene);

        Assert.That(settledScene.Layers, Is.EqualTo([
            new CapitalSceneLayerViewModel(
                new AssetSpriteKey.CapitalBackground(WorldRace.Human, 0),
                AnimationKey: null,
                IsConstructed: false),
            new CapitalSceneLayerViewModel(
                new AssetSpriteKey.CapitalBackground(WorldRace.Human, 1),
                AnimationKey: null,
                IsConstructed: false),
            new CapitalSceneLayerViewModel(
                SpriteKey: null,
                AnimationKey: new AssetAnimationKey.CapitalBackgroundOverlay(WorldRace.Human, 0),
                IsConstructed: false),
            new CapitalSceneLayerViewModel(
                SpriteKey: null,
                AnimationKey: new AssetAnimationKey.CapitalBuilding(towerId),
                IsConstructed: false),
            new CapitalSceneLayerViewModel(
                new AssetSpriteKey.CapitalBackground(WorldRace.Human, 2),
                AnimationKey: null,
                IsConstructed: false),
            new CapitalSceneLayerViewModel(
                SpriteKey: null,
                AnimationKey: new AssetAnimationKey.CapitalBackgroundOverlay(WorldRace.Human, 1),
                IsConstructed: false),
            new CapitalSceneLayerViewModel(
                SpriteKey: null,
                AnimationKey: new AssetAnimationKey.CapitalBuilding(hallId),
                IsConstructed: false)
        ]));
    }
}
