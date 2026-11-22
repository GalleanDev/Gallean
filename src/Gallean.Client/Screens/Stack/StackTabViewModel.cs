using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Engine;

namespace Gallean.Client;

internal abstract partial class StackTabViewModel
    : ManagementTabViewModel, IUnitGridSelectionProvider, IInventoryProvider, IDragDropProvider, IDragDropHost
{
    public enum UnitActionType
    {
        Dismiss,
        LockType
    }

    private const int MaxLeaderNameLength = 20;

    private readonly UnitGridSelectionViewModel unitSelection;
    private WorldStack? currentStack;
    private WorldGetStackInfoCommand.Result stackInfo = new([], OccupiedLeadership: 0, TotalLeadership: 0);

    protected StackTabViewModel(
        GameModel gameModel,
        GameViewModel gameViewModel,
        ManagementScreenViewModel screen,
        LeaderBlankslateViewModel? leaderBlankslate)
        : base(gameModel, gameViewModel)
    {
        Screen = screen;
        LeaderBlankslate = leaderBlankslate;

        unitSelection = new UnitGridSelectionViewModel(gameModel, this);
        UnitGrid = new UnitGridViewModel(gameModel, unitSelection, isFacingLeft: false);
        Equipment = new LeaderEquipmentViewModel(gameModel);
        Inventory = new InventoryViewModel(gameModel, this);
        DragDrop = new DragDropViewModel(gameModel, this);
        DropZones = [SpellDropZone, GroundDropZone, DismissDropZone];

        RegisterSnapshotGatedCommands(DismissCommand, LockTypeCommand);
    }

    public ManagementScreenViewModel Screen { get; }

    public override string TabHeader => Tr("Party");
    public override ResourceSetProjection ResourceProjection => ResourceSetProjection.All;

    public UnitGridViewModel UnitGrid { get; }
    public LeaderEquipmentViewModel Equipment { get; }
    public InventoryViewModel Inventory { get; }
    public DragDropViewModel DragDrop { get; }
    public DropZoneViewModel GroundDropZone { get; } = new();
    public DropZoneViewModel SpellDropZone { get; } = new();
    public DropZoneViewModel DismissDropZone { get; } = new();
    public IReadOnlyList<DropZoneViewModel> DropZones { get; }
    public LeaderBlankslateViewModel? LeaderBlankslate { get; }

    [ObservableProperty]
    public partial bool HasStack { get; private set; }

    [ObservableProperty]
    public partial StackLeaderViewModel? Leader { get; private set; }

    [ObservableProperty]
    public partial string StackGridTitle { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string StackLeadership { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial UnitActionType? TargetedAction { get; private set; }

    [ObservableProperty]
    public partial string? TargetingPromptText { get; private set; }

    async Task IInventoryProvider.InvokeItemAsync(InventorySlotReference source)
    {
        if (currentStack is not null)
        {
            await InvokeItemAsync(currentStack, source);
        }
    }

    async Task IDragDropProvider.DropAsync(IDragSourceReference source, IDragTargetReference target)
    {
        if (currentStack is null)
        {
            return;
        }

        switch (source, target)
        {
            case (UnitGridSlotReference sourceSlot, UnitGridSlotReference targetSlot):
                await unitSelection.DropUnitAsync(sourceSlot, targetSlot);
                break;

            case (UnitGridSlotReference sourceSlot, DropZoneReference zone) when zone.Zone == DismissDropZone:
                await DisbandAsync(currentStack, sourceSlot.Position);
                break;

            case (IItemDragSourceReference itemSource, _):
                await DropItemAsync(currentStack, itemSource, target);
                break;
        }
    }

    void IDragDropProvider.OnDragChanged()
    {
        RefreshView();
    }

    private async Task InvokeItemAsync(WorldStack stack, InventorySlotReference sourceSlot)
    {
        WorldItem? item = sourceSlot.Inventory.GetItem(sourceSlot.Index);

        if (item is not null)
        {
            ItemDefinition definition = GameModel.Definitions.GetItem(item.DefinitionId);

            if (definition.Effect is SpellItemEffect)
            {
                await UseSpellItemAsync(stack, item);

                RefreshView();
            }
        }
    }

    private async Task DropItemAsync(WorldStack stack, IItemDragSourceReference source, IDragTargetReference target)
    {
        WorldItem? item = GetItemAt(stack, source);

        if (item is null)
        {
            return;
        }

        switch (source, target)
        {
            case (_, LeaderEquipmentSlotReference targetSlot) when !source.Equals(targetSlot):
                await TryEquipItemAsync(stack.Id, item.Id, targetSlot.Slot);
                break;

            case (_, UnitGridSlotReference targetSlot):
                await UseItemOnUnitAsync(stack, item, targetSlot.Position);
                break;

            case (LeaderEquipmentSlotReference sourceSlot, InventoryReference):
                await TryDispatchWorldActionAsync(new WorldUnequipItemCommand(stack.Id, sourceSlot.Slot));
                break;

            case (_, DropZoneReference zone) when zone.Zone == GroundDropZone:
                DropOnGround(stack, item);
                break;

            case (_, DropZoneReference zone) when zone.Zone == SpellDropZone:
                await UseSpellItemAsync(stack, item);
                break;
        }

        RefreshView();
    }

    private async Task UseItemOnUnitAsync(WorldStack stack, WorldItem item, GridPosition position)
    {
        WorldUnit? unit = stack.Units[position];

        if (unit is not null)
        {
            await UseItemAsync(stack.Id, item, new WorldItemTarget.Unit(new WorldUnitGroup.Stack(stack.Id), unit.Id));
        }
    }

    private async Task UseItemAsync(WorldStackId stackId, WorldItem item, WorldItemTarget target)
    {
        if (await TryDispatchWorldActionAsync(new WorldUseItemCommand(new WorldUnitGroup.Stack(stackId), item.Id, target)))
        {
            ItemDefinition definition = GameModel.Definitions.GetItem(item.DefinitionId);

            Game.PlaySound(ItemSoundResolver.ResolveWorld(definition.Category));
        }
    }

    private async Task<bool> TryEquipItemAsync(WorldStackId stackId, WorldItemId itemId, LeaderEquipmentSlot slot)
    {
        WorldEquipItemCommand command = new(stackId, itemId, slot);
        WorldCommandResult commandResult = GameModel.World.DryRunCommand(command);

        if (commandResult.Error is WorldCommandError.EquipmentSlotOccupied)
        {
            if (!await TryDispatchWorldActionAsync(new WorldUnequipItemCommand(stackId, slot)))
            {
                return false;
            }
        }

        return await TryDispatchWorldActionAsync(command);
    }

    private void DropOnGround(WorldStack stack, WorldItem item)
    {
        if (Game.RootScreen is WorldScreenViewModel world)
        {
            Screen.Close();
            world.SelectAction(new WorldCursorAction.DropItem(stack.Id, item.Id));
        }
    }

    private async Task UseSpellItemAsync(WorldStack stack, WorldItem item)
    {
        WorldUseItemCommand command = new(new WorldUnitGroup.Stack(stack.Id), item.Id, Target: null);
        WorldCommandResult commandResult = GameModel.World.DryRunCommand(command);

        if (commandResult.Error is WorldCommandError.SpellTargetInvalid)
        {
            if (Game.RootScreen is WorldScreenViewModel world)
            {
                Screen.Close();
                world.SelectAction(new WorldCursorAction.UseItem(stack.Id, item.Id));
            }
        }
        else if (await TryDispatchWorldActionAsync(command))
        {
            ItemDefinition definition = GameModel.Definitions.GetItem(item.DefinitionId);

            Game.PlaySound(ItemSoundResolver.ResolveWorld(definition.Category));
            Screen.Close();
        }
    }

    private static WorldItem? GetItemAt(WorldStack stack, IItemDragSourceReference source)
    {
        return source switch
        {
            InventorySlotReference inventorySlot =>
                inventorySlot.Inventory.GetItem(inventorySlot.Index),
            LeaderEquipmentSlotReference equipmentSlot =>
                stack.Equipment[equipmentSlot.Slot],
            _ => null
        };
    }

    async Task<bool> IUnitGridSelectionProvider.MoveUnitAsync(UnitGridSlotReference source, UnitGridSlotReference target)
    {
        if (currentStack is null)
        {
            return false;
        }

        WorldUnit? unit = currentStack.Units[source.Position];

        if (unit is null)
        {
            return false;
        }

        WorldRepositionUnitCommand command = new(new WorldUnitGroup.Stack(currentStack.Id), unit.Id, target.Position);
        return await TryDispatchWorldActionAsync(command);
    }

    async Task<bool> IUnitGridSelectionProvider.TryInvokeUnitSlotAsync(UnitGridSlotReference target)
    {
        if (TargetedAction is not null && currentStack is not null)
        {
            await ApplyTargetedActionAsync(TargetedAction.Value, currentStack, target.Position);

            return true;
        }
        else
        {
            return false;
        }
    }

    async Task IUnitGridSelectionProvider.InvokeEmptySlotAsync(UnitGridSlotReference target)
    {
        if (TargetedAction is not null)
        {
            CancelTargeting();
        }
    }

    void IUnitGridSelectionProvider.OnUnitSelectionChanged()
    {
        RefreshView();
    }

    void IUnitGridSelectionProvider.OnUnitMoved()
    {
        RefreshView();
    }

    protected abstract void RefreshView();

    protected void ShowStack(WorldStack stack)
    {
        currentStack = stack;
        stackInfo = ResolveStackInfo(stack);
        HasStack = true;

        IDragSourceReference? dragged = DragDrop.Dragged;

        UnitGrid.Update(stack.Units, showsPurchaseHints: false, highlight: BuildUnitHighlight(stack, dragged));

        if (TargetedAction is not null)
        {
            TargetingPromptText = FormatTargetingPrompt(TargetedAction.Value);
        }
        else
        {
            TargetingPromptText = null;
        }

        Inventory.Update(stack.Inventory, WorldState.Players[ViewPlayerId].Resources, IsInventoryDropTarget(stack, dragged));

        StackLeaderViewModel leader = StackLeaderViewModel.Create(
            stack, WorldState.Players[stack.OwnerId], GameModel.Definitions, GameModel.Texts, GameModel.Stats);

        Leader = leader;
        StackGridTitle = Tr("{0}'s party", leader.Title);
        StackLeadership = FormatStackLeadership(stackInfo);

        Equipment.Update(stack, WorldState.Players[ViewPlayerId].Resources, BuildEquipmentHighlight(stack, dragged));

        UpdateDropZones(stack, dragged);
        UpdateDismissDropZone(stack, dragged);
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

    private IReadOnlySet<GridPosition> BuildUnitHighlight(WorldStack stack, IDragSourceReference? dragged)
    {
        return dragged switch
        {
            UnitGridSlotReference source => BuildMoveHighlight(stack, source),
            IItemDragSourceReference source => BuildUseItemHighlight(stack, GetItemAt(stack, source)),
            null when TargetedAction is not null => BuildTargetHighlight(stack, TargetedAction.Value),
            _ => new HashSet<GridPosition>()
        };
    }

    private IReadOnlySet<GridPosition> BuildMoveHighlight(WorldStack stack, UnitGridSlotReference source)
    {
        HashSet<GridPosition> positions = [];
        WorldUnit? unit = stack.Units[source.Position];

        if (unit is null)
        {
            return positions;
        }

        foreach (GridPosition position in GridPosition.All)
        {
            WorldRepositionUnitCommand command = new(new WorldUnitGroup.Stack(stack.Id), unit.Id, position);
            WorldCommandResult result = GameModel.World.DryRunCommand(command);

            if (position != source.Position && !result.Failed)
            {
                positions.Add(position);
            }
        }

        return positions;
    }

    private IReadOnlySet<GridPosition> BuildTargetHighlight(WorldStack stack, UnitActionType type)
    {
        HashSet<GridPosition> positions = [];

        foreach ((WorldUnit unit, GridPosition position) in stack.Units.GetUnits())
        {
            if (IsActionTarget(type, unit))
            {
                positions.Add(position);
            }
        }

        return positions;
    }

    private IReadOnlySet<GridPosition> BuildUseItemHighlight(WorldStack stack, WorldItem? item)
    {
        HashSet<GridPosition> positions = [];

        if (item is null)
        {
            return positions;
        }

        foreach ((WorldUnit unit, GridPosition position) in stack.Units.GetUnits())
        {
            WorldUseItemCommand command = new(new WorldUnitGroup.Stack(stack.Id), item.Id, new WorldItemTarget.Unit(new WorldUnitGroup.Stack(stack.Id), unit.Id));
            WorldCommandResult result = GameModel.World.DryRunCommand(command);

            if (!result.Failed)
            {
                positions.Add(position);
            }
        }

        return positions;
    }

    private IReadOnlySet<LeaderEquipmentSlot> BuildEquipmentHighlight(WorldStack stack, IDragSourceReference? dragged)
    {
        HashSet<LeaderEquipmentSlot> slots = [];

        if (dragged is not IItemDragSourceReference source)
        {
            return slots;
        }

        WorldItem? item = GetItemAt(stack, source);

        if (item is null)
        {
            return slots;
        }

        foreach (LeaderEquipmentSlot slot in Enum.GetValues<LeaderEquipmentSlot>())
        {
            WorldEquipItemCommand command = new(stack.Id, item.Id, slot);
            WorldCommandResult commandResult = GameModel.World.DryRunCommand(command);

            if (!source.Equals(new LeaderEquipmentSlotReference(Equipment, slot)) &&
                commandResult.Error is null or WorldCommandError.EquipmentSlotOccupied)
            {
                slots.Add(slot);
            }
        }

        return slots;
    }

    private bool IsInventoryDropTarget(WorldStack stack, IDragSourceReference? dragged)
    {
        if (dragged is LeaderEquipmentSlotReference source)
        {
            WorldCommandResult result = GameModel.World.DryRunCommand(new WorldUnequipItemCommand(stack.Id, source.Slot));

            return !result.Failed;
        }
        else
        {
            return false;
        }
    }

    private void UpdateDismissDropZone(WorldStack stack, IDragSourceReference? dragged)
    {
        WorldUnit? unit = null;

        if (dragged is UnitGridSlotReference source)
        {
            unit = stack.Units[source.Position];
        }

        if (unit is not null && IsActionTarget(UnitActionType.Dismiss, unit))
        {
            DismissDropZone.Show(Tr("Dismiss {0}", UnitNameResolver.Resolve(unit, GameModel.Definitions, GameModel.Texts)));
        }
        else
        {
            DismissDropZone.Hide();
        }
    }

    private void UpdateDropZones(WorldStack stack, IDragSourceReference? dragged)
    {
        WorldItem? item = null;

        if (dragged is IItemDragSourceReference source)
        {
            item = GetItemAt(stack, source);
        }

        if (item is null)
        {
            GroundDropZone.Hide();
            SpellDropZone.Hide();

            return;
        }

        ItemDefinition definition = GameModel.Definitions.GetItem(item.DefinitionId);
        string name = GameModel.Texts.GetGlobalText(definition.NameTextId).Text;

        if (dragged is InventorySlotReference && Game.RootScreen is WorldScreenViewModel)
        {
            GroundDropZone.Show(Tr("Drop {0}", name));
        }
        else
        {
            GroundDropZone.Hide();
        }

        if (definition.Effect is SpellItemEffect && CanUseItem(stack, item))
        {
            SpellDropZone.Show(Tr("Use {0}", name));
        }
        else
        {
            SpellDropZone.Hide();
        }
    }

    private bool CanUseItem(WorldStack stack, WorldItem item)
    {
        WorldUseItemCommand command = new(new WorldUnitGroup.Stack(stack.Id), item.Id, null);
        WorldCommandResult commandResult = GameModel.World.DryRunCommand(command);

        return commandResult.Error is not (WorldCommandError.MissingLeaderAbility or WorldCommandError.UnitDead);
    }

    protected void ShowNoStack()
    {
        currentStack = null;
        stackInfo = new WorldGetStackInfoCommand.Result([], OccupiedLeadership: 0, TotalLeadership: 0);
        HasStack = false;
        TargetedAction = null;
        TargetingPromptText = null;

        unitSelection.ClearSelection();
        UnitGrid.Update(UnitGrid<WorldUnit>.Empty, showsPurchaseHints: false);
        Inventory.Reset();
        Equipment.Reset();
        GroundDropZone.Hide();
        SpellDropZone.Hide();
        DismissDropZone.Hide();

        Leader = null;
        StackGridTitle = string.Empty;
        StackLeadership = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanTargetUnitAction))]
    private void Dismiss()
    {
        BeginTargeting(UnitActionType.Dismiss);
    }

    [RelayCommand(CanExecute = nameof(CanTargetUnitAction))]
    private void LockType()
    {
        BeginTargeting(UnitActionType.LockType);
    }

    private bool CanTargetUnitAction()
    {
        if (IsBusy)
        {
            return false;
        }

        return true;
    }

    private void BeginTargeting(UnitActionType type)
    {
        if (HasActionTarget(type))
        {
            TargetedAction = type;
            RefreshView();
        }
        else
        {
            IconMoniker icon = type switch
            {
                UnitActionType.Dismiss => IconMoniker.Delete,
                UnitActionType.LockType => IconMoniker.Locked
            };

            string caption = type switch
            {
                UnitActionType.Dismiss => Tr("No units can be dismissed"),
                UnitActionType.LockType => Tr("No units can have their type locked")
            };

            Game.ShowInfo(icon, caption, null);
        }
    }

    public override bool TryCancel()
    {
        if (TargetedAction is not null)
        {
            CancelTargeting();

            return true;
        }
        else
        {
            return false;
        }
    }

    [RelayCommand]
    private void CancelTargeting()
    {
        TargetedAction = null;
        RefreshView();
    }

    protected override void OnSelectionChanged(bool isSelected)
    {
        if (!isSelected && TargetedAction is not null)
        {
            TargetedAction = null;
            RefreshView();
        }
    }

    private static string FormatTargetingPrompt(UnitActionType type)
    {
        return type switch
        {
            UnitActionType.Dismiss => Tr("Select a unit to dismiss it."),
            UnitActionType.LockType => Tr("Select a unit to lock its type.")
        };
    }

    private bool HasActionTarget(UnitActionType type)
    {
        foreach (WorldUnitActionInfo unit in stackInfo.Units)
        {
            if (GetActionError(type, unit) is null)
            {
                return true;
            }
        }

        return false;
    }

    private static WorldCommandError? GetActionError(UnitActionType type, WorldUnitActionInfo unit)
    {
        return type switch
        {
            UnitActionType.Dismiss => unit.DisbandError,
            UnitActionType.LockType => unit.LockTypeError
        };
    }

    private async Task ApplyTargetedActionAsync(UnitActionType type, WorldStack stack, GridPosition position)
    {
        Task action = type switch
        {
            UnitActionType.Dismiss => DisbandAsync(stack, position),
            UnitActionType.LockType => LockTypeAsync(stack, position)
        };

        await action;
    }

    private bool IsActionTarget(UnitActionType type, WorldUnit unit)
    {
        WorldUnitActionInfo? unitInfo = FindStackUnit(stackInfo, unit.Id);

        return unitInfo is not null && GetActionError(type, unitInfo) is null;
    }

    private async Task LockTypeAsync(WorldStack stack, GridPosition position)
    {
        WorldUnit? unit = stack.Units[position];

        if (unit is not null)
        {
            WorldLockUnitTypeCommand command = new(new WorldUnitGroup.Stack(stack.Id), unit.Id);

            if (IsActionTarget(UnitActionType.LockType, unit))
            {
                Game.OpenScreen(new ConfirmPromptViewModel(
                    TrSpans("Lock unit type"),
                    TrSpans("Lock the type of {0:*}? It will never upgrade to another type and will gain levels instead. This cannot be undone.", UnitNameResolver.Resolve(unit, GameModel.Definitions, GameModel.Texts)),
                    Tr("Lock"),
                    ConfirmPromptType.Danger,
                    () => DispatchLockTypeAsync(command)));
            }
            else
            {
                await DispatchLockTypeAsync(command);
            }
        }
    }

    private async Task DispatchLockTypeAsync(WorldLockUnitTypeCommand command)
    {
        await TryDispatchWorldActionAsync(command);
        RefreshView();
    }

    private async Task DisbandAsync(WorldStack stack, GridPosition position)
    {
        WorldUnit? unit = stack.Units[position];

        if (unit is not null)
        {
            WorldDisbandUnitCommand command = new(new WorldUnitGroup.Stack(stack.Id), unit.Id);

            if (IsActionTarget(UnitActionType.Dismiss, unit))
            {
                Game.OpenScreen(new ConfirmPromptViewModel(
                    TrSpans("Dismiss unit"),
                    TrSpans("Are you sure you want to dismiss {0:*}?", UnitNameResolver.Resolve(unit, GameModel.Definitions, GameModel.Texts)),
                    Tr("Dismiss"),
                    ConfirmPromptType.Danger,
                    () => DispatchDisbandAsync(command)));
            }
            else
            {
                await DispatchDisbandAsync(command);
            }
        }
    }

    private async Task DispatchDisbandAsync(WorldDisbandUnitCommand command)
    {
        if (await TryDispatchWorldActionAsync(command))
        {
            unitSelection.ClearSelection();
        }

        RefreshView();
    }

    [RelayCommand]
    private void RenameLeader()
    {
        if (currentStack is not null && Leader is not null)
        {
            WorldStackId stackId = currentStack.Id;

            Game.OpenScreen(new TextPromptViewModel(
                TrSpans("Hero name"),
                Tr("Enter a new name for your hero."),
                Leader.Title,
                Tr("Hero name"),
                MaxLeaderNameLength,
                name => RenameLeaderConfirmedAsync(stackId, name)));
        }
    }

    private async Task RenameLeaderConfirmedAsync(WorldStackId stackId, string name)
    {
        await TryDispatchWorldActionAsync(new WorldRenameLeaderCommand(stackId, name));
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
}
