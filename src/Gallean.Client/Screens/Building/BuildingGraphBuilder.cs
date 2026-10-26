using Avalonia;
using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed class BuildingGraphBuilder(
    IGameDefinitionProvider definitions,
    IGameTextProvider texts,
    IUnitStatsProvider stats,
    LordDefinition lord,
    WorldPlayerBuildings buildings,
    WorldResourceSet resources,
    WorldGetBuildingsInfoCommand.Result info)
{
    private const double CommonSpacing = 1.6;
    private const double CommonDiameter = 1.3125;

    private const double RootRadius = 3.4;
    private const double ExtentMargin = 0.6;
    private const double ForkInset = 0.6;
    private const double LevelStep = 1.25;
    private const double ProjectionY = 0.8;
    private const double SectorHalfSpanDegrees = 38.0;

    private const double HubRadiusX = 1.7;
    private const double HubRadiusY = 1.9;
    private const double HubRowPadding = 1.1;
    private const double HubEntryRadius = 115.75 / 1024.0;
    private const double HubSize = CommonDiameter / 2.0 / HubEntryRadius;

    private static readonly IReadOnlyList<Point> HubEntries =
    [
        new(0.0, -131.3 / 1024.0),
        new(-178.0 / 1024.0, 65.7 / 1024.0),
        new(178.0 / 1024.0, 65.7 / 1024.0)
    ];

    private readonly IReadOnlyList<WorldPlayerBuildingDefinition> available = CollectBuildings(definitions, lord, info);
    private readonly IReadOnlyDictionary<WorldPlayerBuildingDefinitionId, WorldGetBuildingsInfoCommand.Building> buildingInfos = CollectBuildingInfos(info);

    public BuildingGraphViewModel Build()
    {
        if (available.Count <= 0)
        {
            return BuildingGraphViewModel.Empty;
        }

        IReadOnlyDictionary<WorldPlayerBuildingDefinitionId, BuildingBranchType?> branches = ResolveBranches();
        List<int> levels = CollectLevels(branches);
        IReadOnlyList<WorldPlayerBuildingDefinition> commons = CollectMembers(null, branches);

        double hubRadiusX = Math.Max(HubRadiusX, (commons.Count - 1) / 2.0 * CommonSpacing + HubRowPadding);
        double hubSize = commons.Count == HubEntries.Count ? HubSize : 0.0;
        List<BuildingNodeViewModel> nodes = [.. ProcessCommons(commons, hubSize)];
        List<BuildingLink> links = [];
        List<BuildingBranchViewModel> legend = [];

        foreach (BuildingBranchType type in Enum.GetValues<BuildingBranchType>())
        {
            IReadOnlyList<WorldPlayerBuildingDefinition> members = CollectMembers(type, branches);

            if (members.Count > 0)
            {
                double forkAngle = double.DegreesToRadians(GetSectorCenterDegrees(type));
                double forkX = Math.Round(Math.Cos(forkAngle) * (RootRadius - ForkInset), 2);
                double forkY = Math.Round(Math.Sin(forkAngle) * (RootRadius - ForkInset) * ProjectionY, 2);
                BuildingBranchViewModel branch = new(type, forkX, forkY);
                (List<BuildingNodeViewModel> branchNodes, List<BuildingLink> branchLinks) = ProcessBranch(branch, members, levels);

                nodes.AddRange(branchNodes);
                links.AddRange(branchLinks);
                legend.Add(branch);
            }
        }

        List<BuildingRingViewModel> rings = [];

        for (int index = 0; index < levels.Count; index++)
        {
            double radius = GetRingRadius(index);

            rings.Add(new BuildingRingViewModel(radius, Tr("Level {0}", levels[index])));
        }

        double outerRadius = levels.Count == 0 ? 0.0 : GetRingRadius(levels.Count - 1);

        return new BuildingGraphViewModel(
            nodes,
            links,
            rings,
            legend,
            hubRadiusX,
            HubRadiusY,
            hubSize,
            ProjectionY,
            Math.Max(outerRadius, hubRadiusX) + ExtentMargin,
            Math.Max(outerRadius * ProjectionY, HubRadiusY) + ExtentMargin);
    }

    private static IReadOnlyList<WorldPlayerBuildingDefinition> CollectBuildings(
        IGameDefinitionProvider definitions,
        LordDefinition lord,
        WorldGetBuildingsInfoCommand.Result info)
    {
        List<WorldPlayerBuildingDefinitionId> ids = [.. lord.BuildingIds];

        foreach (WorldGetBuildingsInfoCommand.Building buildingInfo in info.Available)
        {
            if (!ids.Contains(buildingInfo.BuildingId))
            {
                ids.Add(buildingInfo.BuildingId);
            }
        }

        List<WorldPlayerBuildingDefinition> buildings = [];

        foreach (WorldPlayerBuildingDefinitionId id in ids)
        {
            buildings.Add(definitions.GetPlayerBuilding(id));
        }

        return buildings;
    }

    private static IReadOnlyDictionary<WorldPlayerBuildingDefinitionId, WorldGetBuildingsInfoCommand.Building> CollectBuildingInfos(
        WorldGetBuildingsInfoCommand.Result info)
    {
        Dictionary<WorldPlayerBuildingDefinitionId, WorldGetBuildingsInfoCommand.Building> buildingInfos = [];

        foreach (WorldGetBuildingsInfoCommand.Building buildingInfo in info.Available)
        {
            buildingInfos.Add(buildingInfo.BuildingId, buildingInfo);
        }

        return buildingInfos;
    }

    private static double GetRingRadius(int ringIndex)
    {
        return RootRadius + ringIndex * LevelStep;
    }

    private static double GetSectorCenterDegrees(BuildingBranchType branch)
    {
        return branch switch
        {
            BuildingBranchType.Fighter => 228.0,
            BuildingBranchType.Ranger => 312.0,
            BuildingBranchType.Mage => 132.0,
            BuildingBranchType.Special => 48.0
        };
    }

    private static List<WorldPlayerBuildingDefinition> CollectChildren(
        WorldPlayerBuildingDefinition building,
        IReadOnlyList<WorldPlayerBuildingDefinition> members)
    {
        List<WorldPlayerBuildingDefinition> children = [];

        foreach (WorldPlayerBuildingDefinition candidate in members)
        {
            if (candidate.RequiredId == building.Id)
            {
                children.Add(candidate);
            }
        }

        children.Sort(CompareBuildings);

        return children;
    }

    private static int CountLeaves(WorldPlayerBuildingDefinition building, IReadOnlyList<WorldPlayerBuildingDefinition> members)
    {
        List<WorldPlayerBuildingDefinition> children = CollectChildren(building, members);

        if (children.Count == 0)
        {
            return 1;
        }

        int leaves = 0;

        foreach (WorldPlayerBuildingDefinition child in children)
        {
            leaves += CountLeaves(child, members);
        }

        return leaves;
    }

    private static bool ContainsId(IReadOnlyList<WorldPlayerBuildingDefinition> members, WorldPlayerBuildingDefinitionId? id)
    {
        foreach (WorldPlayerBuildingDefinition member in members)
        {
            if (member.Id == id)
            {
                return true;
            }
        }

        return false;
    }

    private IReadOnlyDictionary<WorldPlayerBuildingDefinitionId, BuildingBranchType?> ResolveBranches()
    {
        Dictionary<WorldPlayerBuildingDefinitionId, BuildingBranchType?> branches = [];

        foreach (WorldPlayerBuildingDefinition building in available)
        {
            branches.Add(building.Id, ResolveBranch(building));
        }

        return branches;
    }

    private BuildingBranchType? ResolveBranch(WorldPlayerBuildingDefinition building)
    {
        if (IsSideshow(building) && building.RequiredId is not null)
        {
            WorldPlayerBuildingDefinition prerequisite = definitions.GetPlayerBuilding(building.RequiredId.Value);

            return ResolveBranch(prerequisite);
        }
        else
        {
            return building.UnitBranch switch
            {
                UnitBranch.Fighter => BuildingBranchType.Fighter,
                UnitBranch.Ranger => BuildingBranchType.Ranger,
                UnitBranch.Mage => BuildingBranchType.Mage,
                UnitBranch.Special => BuildingBranchType.Special,
                UnitBranch.Sideshow => null,
                UnitBranch.Leader => null,
                null => null
            };
        }
    }

    private List<int> CollectLevels(IReadOnlyDictionary<WorldPlayerBuildingDefinitionId, BuildingBranchType?> branches)
    {
        List<int> levels = [];

        foreach (WorldPlayerBuildingDefinition building in available)
        {
            if (branches[building.Id] is not null && !levels.Contains(building.Level))
            {
                levels.Add(building.Level);
            }
        }

        levels.Sort();

        return levels;
    }

    private IReadOnlyList<WorldPlayerBuildingDefinition> CollectMembers(
        BuildingBranchType? branch,
        IReadOnlyDictionary<WorldPlayerBuildingDefinitionId, BuildingBranchType?> branches)
    {
        List<WorldPlayerBuildingDefinition> members = [];

        foreach (WorldPlayerBuildingDefinition building in available)
        {
            if (branches[building.Id] == branch)
            {
                members.Add(building);
            }
        }

        members.Sort(CompareBuildings);

        return members;
    }

    private List<BuildingNodeViewModel> ProcessCommons(IReadOnlyList<WorldPlayerBuildingDefinition> commons, double hubSize)
    {
        List<BuildingNodeViewModel> nodes = [];

        if (hubSize > 0.0)
        {
            for (int index = 0; index < commons.Count; index++)
            {
                Point entry = HubEntries[index];
                double x = Math.Round(entry.X * hubSize, 2);
                double y = Math.Round(entry.Y * hubSize, 2);

                nodes.Add(BuildNode(commons[index], branch: null, isBranchRoot: false, x, y));
            }
        }
        else
        {
            for (int index = 0; index < commons.Count; index++)
            {
                double x = (index - (commons.Count - 1) / 2.0) * CommonSpacing;

                nodes.Add(BuildNode(commons[index], branch: null, isBranchRoot: false, x, 0.0));
            }
        }

        return nodes;
    }

    private (List<BuildingNodeViewModel> Nodes, List<BuildingLink> Links) ProcessBranch(
        BuildingBranchViewModel branch,
        IReadOnlyList<WorldPlayerBuildingDefinition> members,
        List<int> levels)
    {
        List<WorldPlayerBuildingDefinition> roots = [];
        int totalLeaves = 0;

        foreach (WorldPlayerBuildingDefinition member in members)
        {
            if (!ContainsId(members, member.RequiredId))
            {
                roots.Add(member);
                totalLeaves += CountLeaves(member, members);
            }
        }

        double slotDegrees = 2.0 * SectorHalfSpanDegrees / totalLeaves;
        double startDegrees = GetSectorCenterDegrees(branch.Type) - SectorHalfSpanDegrees;
        List<BuildingNodeViewModel> nodes = [];
        List<BuildingLink> links = [];
        int firstLeaf = 0;

        foreach (WorldPlayerBuildingDefinition root in roots)
        {
            (List<BuildingNodeViewModel> subtreeNodes, List<BuildingLink> subtreeLinks) =
                ProcessSubtree(root, branch, true, firstLeaf, members, levels, startDegrees, slotDegrees);

            nodes.AddRange(subtreeNodes);
            links.AddRange(subtreeLinks);
            firstLeaf += CountLeaves(root, members);
        }

        return (nodes, links);
    }

    private (List<BuildingNodeViewModel> Nodes, List<BuildingLink> Links) ProcessSubtree(
        WorldPlayerBuildingDefinition building,
        BuildingBranchViewModel branch,
        bool isBranchRoot,
        int firstLeaf,
        IReadOnlyList<WorldPlayerBuildingDefinition> members,
        List<int> levels,
        double startDegrees,
        double slotDegrees)
    {
        List<WorldPlayerBuildingDefinition> children = CollectChildren(building, members);
        int leaves = CountLeaves(building, members);
        double angle = double.DegreesToRadians(startDegrees + slotDegrees * (firstLeaf + leaves / 2.0));
        double radius = GetRingRadius(levels.IndexOf(building.Level));
        double x = Math.Round(Math.Cos(angle) * radius, 2);
        double y = Math.Round(Math.Sin(angle) * radius * ProjectionY, 2);

        BuildingNodeViewModel node = BuildNode(building, branch, isBranchRoot, x, y);
        List<BuildingNodeViewModel> nodes = [node];
        List<BuildingLink> links = [];
        int childFirstLeaf = firstLeaf;

        foreach (WorldPlayerBuildingDefinition child in children)
        {
            (List<BuildingNodeViewModel> childNodes, List<BuildingLink> childLinks) =
                ProcessSubtree(child, branch, isBranchRoot: false, childFirstLeaf, members, levels, startDegrees, slotDegrees);

            links.Add(new BuildingLink(node, childNodes[0]));
            nodes.AddRange(childNodes);
            links.AddRange(childLinks);
            childFirstLeaf += CountLeaves(child, members);
        }

        return (nodes, links);
    }

    private BuildingNodeViewModel BuildNode(
        WorldPlayerBuildingDefinition building,
        BuildingBranchViewModel? branch,
        bool isBranchRoot,
        double x,
        double y)
    {
        string name = texts.GetGlobalText(building.NameTextId).Text;
        BuildingStatus status;
        TextSpans? lockedText;
        IReadOnlyList<WorldPlayerBuildingDefinitionId> lockedIds;

        if (buildingInfos.TryGetValue(building.Id, out WorldGetBuildingsInfoCommand.Building? buildingInfo))
        {
            status = ResolveStatus(building, buildingInfo.Error);
            lockedText = FormatLockedText(buildingInfo.LockedIds);
            lockedIds = CollectLockedIds(buildingInfo.LockedIds);
        }
        else
        {
            status = new BuildingStatus.ScenarioLocked();
            lockedText = null;
            lockedIds = [];
        }

        return new BuildingNodeViewModel(
            building.Id,
            name,
            texts.GetGlobalText(building.DescriptionTextId).Text,
            branch is null ? Tr("Building") : Tr("Level {0}", building.Level),
            building.Cost,
            resources,
            status,
            BuildUnits(building),
            branch,
            isBranchRoot,
            x,
            y,
            lockedText,
            lockedIds);
    }

    private IReadOnlyList<WorldPlayerBuildingDefinitionId> CollectLockedIds(IReadOnlyList<WorldPlayerBuildingDefinitionId> lockedIds)
    {
        List<WorldPlayerBuildingDefinitionId> result = [];

        Queue<WorldPlayerBuildingDefinitionId> queue = new(lockedIds);

        while (queue.TryDequeue(out WorldPlayerBuildingDefinitionId lockedId))
        {
            result.Add(lockedId);

            foreach (WorldPlayerBuildingDefinition building in available)
            {
                if (building.RequiredId == lockedId)
                {
                    queue.Enqueue(building.Id);
                }
            }
        }

        return result;
    }

    private TextSpans? FormatLockedText(IReadOnlyList<WorldPlayerBuildingDefinitionId> lockedIds)
    {
        if (lockedIds.Count > 0)
        {
            JoinStringBuilder names = new(", ");

            foreach (WorldPlayerBuildingDefinitionId lockedId in lockedIds)
            {
                WorldPlayerBuildingDefinition locked = definitions.GetPlayerBuilding(lockedId);

                names.Add(texts.GetGlobalText(locked.NameTextId).Text);
            }

            return TrSpans("Permanently locks out {0:i}", names.ToString());
        }
        else
        {
            return null;
        }
    }

    private BuildingStatus ResolveStatus(WorldPlayerBuildingDefinition building, WorldCommandError? error)
    {
        if (buildings.Acquired.Contains(building.Id))
        {
            return new BuildingStatus.Built();
        }
        else
        {
            switch (error)
            {
                case null:
                    return new BuildingStatus.Constructible();
                case WorldCommandError.BuildingBranchLocked:
                    return new BuildingStatus.BranchLocked();
                case WorldCommandError.BuildingMissingPrerequisite when building.RequiredId is not null:
                    WorldPlayerBuildingDefinition required = definitions.GetPlayerBuilding(building.RequiredId.Value);
                    return new BuildingStatus.MissingPrerequisite(texts.GetGlobalText(required.NameTextId).Text);
                default:
                    return new BuildingStatus.Unavailable(WorldCommandErrorFormatter.Format(error.Value));
            }
        }
    }

    private IReadOnlyList<BuildingUnitViewModel> BuildUnits(WorldPlayerBuildingDefinition building)
    {
        List<BuildingUnitViewModel> units = [];

        foreach (UnitDefinitionId unitId in building.UnitIds)
        {
            UnitDefinition? unitDefinition = definitions.TryGetUnit(unitId);

            if (unitDefinition is not null)
            {
                WorldUnit unit = new(UnitDetailsViewModel.PreviewUnitId, unitId, unitDefinition.MaxHitpoints, Experience: 0, unitDefinition.Level, [], null, false);

                units.Add(new BuildingUnitViewModel(
                    new AssetSpriteKey.UnitFace(unitId),
                    new AssetSpriteKey.UnitPortrait(unitId),
                    texts.GetGlobalText(unitDefinition.NameTextId).Text,
                    UnitDetailsViewModel.CreateFromWorld(unit, null, definitions, texts, stats)));
            }
        }

        return units;
    }

    private static bool IsSideshow(WorldPlayerBuildingDefinition building)
    {
        return building.UnitBranch is UnitBranch.Sideshow or UnitBranch.Leader;
    }

    private static int CompareBuildings(WorldPlayerBuildingDefinition left, WorldPlayerBuildingDefinition right)
    {
        int rv;

        rv = IsSideshow(left).CompareTo(IsSideshow(right));
        if (rv != 0)
        {
            return rv;
        }

        rv = left.Id.CompareTo(right.Id);
        if (rv != 0)
        {
            return rv;
        }

        return 0;
    }
}
