using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class ExchangeTabViewModel
    : ManagementTabViewModel, IUnitGridSelectionProvider, IInventoryTransferProvider, IDragDropProvider, IDragDropHost
{
    private readonly ManagementScreenViewModel screen;
    private readonly WorldStackId stackId;
    private readonly UnitGridSelectionViewModel unitSelection;
    private WorldStack? stack;
    private WorldGetStackInfoCommand.Result stackInfo = new([], OccupiedLeadership: 0, TotalLeadership: 0);
    private readonly WorldStackId otherStackId;
    private WorldStack? otherStack;
    private WorldGetStackInfoCommand.Result otherStackInfo = new([], OccupiedLeadership: 0, TotalLeadership: 0);

    public ExchangeTabViewModel(GameModel gameModel, GameViewModel gameViewModel, ManagementScreenViewModel screen, WorldStackId stackId, WorldStackId otherStackId)
        : base(gameModel, gameViewModel)
    {
        this.screen = screen;
        this.stackId = stackId;
        this.otherStackId = otherStackId;

        unitSelection = new UnitGridSelectionViewModel(gameModel, this);
        StackGrid = new UnitGridViewModel(gameModel, unitSelection, frontOnRight: true);
        OtherStackGrid = new UnitGridViewModel(gameModel, unitSelection, frontOnRight: false);
        InventoryTransfer = new InventoryTransferViewModel(gameModel, this);
        DragDrop = new DragDropViewModel(gameModel, this);
        DropZones = [DismissDropZone];

        RefreshView();
    }

    public override string TabHeader => Tr("Exchange");

    public UnitGridViewModel StackGrid { get; }
    public UnitGridViewModel OtherStackGrid { get; }
    public InventoryTransferViewModel InventoryTransfer { get; }
    public DragDropViewModel DragDrop { get; }
    public DropZoneViewModel DismissDropZone { get; } = new();
    public IReadOnlyList<DropZoneViewModel> DropZones { get; }

    [ObservableProperty]
    public partial StackLeaderViewModel? Leader { get; private set; }

    [ObservableProperty]
    public partial StackLeaderViewModel? OtherLeader { get; private set; }

    [ObservableProperty]
    public partial string StackGridTitle { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string StackUnitCount { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string OtherStackGridTitle { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string OtherStackUnitCount { get; private set; } = string.Empty;

    void IInventoryTransferProvider.OnItemTransferred()
    {
        RefreshView();
    }

    async Task<bool> IInventoryTransferProvider.TransferItemAsync(WorldItemId itemId, InventoryTransferDirection direction)
    {
        return await TryDispatchWorldActionAsync(BuildTransferCommand(itemId, direction));
    }

    async Task IDragDropProvider.DropAsync(IDragSourceReference source, IDragTargetReference target)
    {
        switch (source, target)
        {
            case (UnitGridSlotReference sourceSlot, UnitGridSlotReference targetSlot):
                await unitSelection.DropUnitAsync(sourceSlot, targetSlot);
                break;

            case (InventorySlotReference sourceSlot, InventoryReference targetInventory):
                await InventoryTransfer.DropItemAsync(sourceSlot, targetInventory);
                break;

            case (UnitGridSlotReference sourceSlot, DropZoneReference zone) when zone.Zone == DismissDropZone:
                ConfirmDisband(sourceSlot);
                break;
        }
    }

    void IDragDropProvider.OnDragChanged()
    {
        RefreshView();
    }

    void IUnitGridSelectionProvider.OnUnitSelectionChanged()
    {
        RefreshView();
    }

    void IUnitGridSelectionProvider.OnUnitMoved()
    {
        RefreshView();
    }

    async Task<bool> IUnitGridSelectionProvider.MoveUnitAsync(UnitGridSlotReference source, UnitGridSlotReference target)
    {
        WorldStack? sourceStack = ResolveStack(source.Grid);
        WorldUnit? unit = sourceStack?.Units[source.Position];

        if (unit is not null)
        {
            return await TryDispatchWorldActionAsync(BuildMove(unit, source, target));
        }
        else
        {
            return false;
        }
    }

    private void ConfirmDisband(UnitGridSlotReference slot)
    {
        WorldStack? slotStack = ResolveStack(slot.Grid);
        WorldUnit? unit = slotStack?.Units[slot.Position];

        if (slotStack is not null && unit is not null)
        {
            WorldDisbandUnitCommand command = new(new WorldUnitGroup.Stack(slotStack.Id), unit.Id);

            Game.OpenScreen(new ConfirmPromptViewModel(
                Tr("Dismiss unit"),
                TrSpans("Are you sure you want to dismiss {0:*}?", UnitNameResolver.Resolve(unit, GameModel.Definitions, GameModel.Texts)),
                Tr("Dismiss"),
                ConfirmPromptType.Danger,
                () => DisbandConfirmedAsync(command)));
        }
    }

    private bool CanDisband(UnitGridSlotReference slot)
    {
        WorldStack? slotStack = ResolveStack(slot.Grid);
        WorldUnit? unit = slotStack?.Units[slot.Position];

        if (unit is not null)
        {
            WorldUnitActionInfo? unitInfo = FindStackUnit(ResolveStackInfo(slot.Grid), unit.Id);

            return unitInfo is not null && unitInfo.DisbandError is null;
        }
        else
        {
            return false;
        }
    }

    private async Task DisbandConfirmedAsync(WorldDisbandUnitCommand command)
    {
        if (await TryDispatchWorldActionAsync(command))
        {
            unitSelection.ClearSelection();
        }

        RefreshView();
    }

    [RelayCommand]
    private void ClearSelection()
    {
        unitSelection.ClearSelection();
        RefreshView();
    }

    protected override void OnSnapshotChanged(GameSnapshot current)
    {
        RefreshView();
    }

    private IWorldCommand<Empty> BuildMove(WorldUnit unit, UnitGridSlotReference source, UnitGridSlotReference target)
    {
        bool sourceIsStack = source.Grid == StackGrid;
        bool targetIsStack = target.Grid == StackGrid;

        if (sourceIsStack == targetIsStack)
        {
            WorldStackId repositionStackId;

            if (sourceIsStack)
            {
                repositionStackId = stackId;
            }
            else
            {
                repositionStackId = otherStackId;
            }

            return new WorldRepositionUnitCommand(new WorldUnitGroup.Stack(repositionStackId), unit.Id, target.Position);
        }
        else
        {
            WorldUnitGroup stackTarget = new WorldUnitGroup.Stack(stackId);
            WorldUnitGroup otherStackTarget = new WorldUnitGroup.Stack(otherStackId);

            if (sourceIsStack)
            {
                return new WorldTransferUnitCommand(stackTarget, otherStackTarget, unit.Id, target.Position);
            }
            else
            {
                return new WorldTransferUnitCommand(otherStackTarget, stackTarget, unit.Id, target.Position);
            }
        }
    }

    private WorldTransferInventoryItemCommand BuildTransferCommand(WorldItemId itemId, InventoryTransferDirection direction)
    {
        WorldUnitGroup stackTarget = new WorldUnitGroup.Stack(stackId);
        WorldUnitGroup otherStackTarget = new WorldUnitGroup.Stack(otherStackId);

        return direction switch
        {
            InventoryTransferDirection.ToOther => new WorldTransferInventoryItemCommand(stackTarget, otherStackTarget, itemId),
            InventoryTransferDirection.FromOther => new WorldTransferInventoryItemCommand(otherStackTarget, stackTarget, itemId)
        };
    }

    private IReadOnlySet<GridPosition> BuildUnitHighlight(UnitGridViewModel grid, IDragSourceReference? dragged)
    {
        HashSet<GridPosition> positions = [];

        if (dragged is not UnitGridSlotReference source)
        {
            return positions;
        }

        WorldStack? sourceStack = ResolveStack(source.Grid);
        WorldUnit? unit = sourceStack?.Units[source.Position];

        if (unit is null)
        {
            return positions;
        }

        foreach (GridPosition position in GridPosition.All)
        {
            UnitGridSlotReference target = new(grid, position);
            WorldCommandResult result = GameModel.World.DryRunCommand(BuildMove(unit, source, target));

            if (target != source && !result.Failed)
            {
                positions.Add(position);
            }
        }

        return positions;
    }

    private InventoryTransferDirection? BuildTransferHighlight(IDragSourceReference? dragged)
    {
        if (dragged is not InventorySlotReference source)
        {
            return null;
        }

        WorldItem? item = source.Inventory.GetItem(source.Index);
        InventoryTransferDirection? direction = InventoryTransfer.GetTransferDirection(source);

        if (item is not null && direction is not null)
        {
            WorldTransferInventoryItemCommand command = BuildTransferCommand(item.Id, direction.Value);
            WorldCommandResult commandResult = GameModel.World.DryRunCommand(command);

            if (!commandResult.Failed)
            {
                return direction;
            }
        }

        return null;
    }

    private void RefreshView()
    {
        stack = ResolveOwnedStack(stackId);
        otherStack = ResolveOwnedStack(otherStackId);

        if (stack is null || otherStack is null || !stack.Position.IsAdjacentTo(otherStack.Position))
        {
            screen.Close();

            return;
        }

        stackInfo = ResolveStackInfo(stack);
        otherStackInfo = ResolveStackInfo(otherStack);

        IDragSourceReference? dragged = DragDrop.Dragged;

        StackGrid.Update(stack.Units, showsPurchaseHints: false, BuildUnitHighlight(StackGrid, dragged));
        OtherStackGrid.Update(otherStack.Units, showsPurchaseHints: false, BuildUnitHighlight(OtherStackGrid, dragged));
        InventoryTransfer.Update(stack.Inventory, otherStack.Inventory, BuildTransferHighlight(dragged));

        StackLeaderViewModel leader = StackLeaderViewModel.Create(
            stack, WorldState.Players[stack.OwnerId], GameModel.Definitions, GameModel.Texts, GameModel.Stats);
        StackLeaderViewModel otherLeader = StackLeaderViewModel.Create(
            otherStack, WorldState.Players[otherStack.OwnerId], GameModel.Definitions, GameModel.Texts, GameModel.Stats);

        Leader = leader;
        OtherLeader = otherLeader;
        StackGridTitle = Tr("{0}'s party", leader.Title);
        OtherStackGridTitle = Tr("{0}'s party", otherLeader.Title);
        StackUnitCount = FormatStackLeadership(stackInfo);
        OtherStackUnitCount = FormatStackLeadership(otherStackInfo);

        UpdateDismissDropZone(dragged);
    }

    private void UpdateDismissDropZone(IDragSourceReference? dragged)
    {
        WorldUnit? unit = null;

        if (dragged is UnitGridSlotReference source && CanDisband(source))
        {
            WorldStack? sourceStack = ResolveStack(source.Grid);

            unit = sourceStack?.Units[source.Position];
        }

        if (unit is not null)
        {
            DismissDropZone.Show(Tr("Dismiss {0}", UnitNameResolver.Resolve(unit, GameModel.Definitions, GameModel.Texts)));
        }
        else
        {
            DismissDropZone.Hide();
        }
    }

    private WorldStack? ResolveStack(UnitGridViewModel grid)
    {
        if (grid == StackGrid)
        {
            return stack;
        }
        else
        {
            return otherStack;
        }
    }

    private WorldGetStackInfoCommand.Result ResolveStackInfo(UnitGridViewModel grid)
    {
        if (grid == StackGrid)
        {
            return stackInfo;
        }
        else
        {
            return otherStackInfo;
        }
    }

    private WorldGetStackInfoCommand.Result ResolveStackInfo(WorldStack stack)
    {
        ICommandResult<WorldCommandError, WorldEvent, WorldGetStackInfoCommand.Result> result =
            GameModel.World.DryRunCommand(new WorldGetStackInfoCommand(stack.Id));

        if (result.Failed)
        {
            return new WorldGetStackInfoCommand.Result([], OccupiedLeadership: 0, TotalLeadership: 0);
        }
        else
        {
            return result.Data;
        }
    }

    private static string FormatStackLeadership(WorldGetStackInfoCommand.Result info)
    {
        return $"{info.OccupiedLeadership} / {info.TotalLeadership}";
    }

    private static WorldUnitActionInfo? FindStackUnit(WorldGetStackInfoCommand.Result info, WorldUnitId unitId)
    {
        foreach (WorldUnitActionInfo unit in info.Units)
        {
            if (unit.UnitId == unitId)
            {
                return unit;
            }
        }

        return null;
    }

    private WorldStack? ResolveOwnedStack(WorldStackId id)
    {
        if (WorldState.Stacks.TryGetValue(id, out WorldStack? resolved) && resolved.OwnerId == ViewPlayerId)
        {
            return resolved;
        }
        else
        {
            return null;
        }
    }
}
