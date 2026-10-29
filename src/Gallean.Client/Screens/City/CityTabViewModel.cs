using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class CityTabViewModel
    : ManagementTabViewModel, IUnitGridSelectionProvider, IInventoryTransferProvider, IDragDropProvider, IDragDropHost
{
    public enum UnitActionType
    {
        Heal,
        Resurrect,
        LockType
    }

    private readonly WorldFortificationId fortificationId;
    private readonly ManagementScreenViewModel screen;
    private readonly UnitGridSelectionViewModel unitSelection;
    private WorldFortification? fortification;
    private WorldStack? visitingStack;
    private WorldGetCityInfoCommand.Result cityInfo = new(Growth: null, HealError: null);
    private WorldGetStackInfoCommand.Result stackInfo = new([], OccupiedLeadership: 0, TotalLeadership: 0);
    private WorldGetGarrisonInfoCommand.Result garrisonInfo = new([], OccupiedCapacity: 0, TotalCapacity: 0);

    public CityTabViewModel(GameModel gameModel, GameViewModel gameViewModel, ManagementScreenViewModel screen, WorldFortificationId fortificationId)
        : base(gameModel, gameViewModel)
    {
        this.screen = screen;
        this.fortificationId = fortificationId;

        unitSelection = new UnitGridSelectionViewModel(gameModel, this);
        StackGrid = new UnitGridViewModel(gameModel, unitSelection, isFacingLeft: false);
        GarrisonGrid = new UnitGridViewModel(gameModel, unitSelection, isFacingLeft: true);
        InventoryTransfer = new InventoryTransferViewModel(gameModel, this);
        DragDrop = new DragDropViewModel(gameModel, this);
        DropZones = [SpellDropZone, DismissDropZone];
        LeaderBlankslate = new LeaderBlankslateViewModel(gameModel, gameViewModel, fortificationId);

        RefreshView();

        RegisterSnapshotGatedCommands(
            HealCommand,
            ResurrectCommand,
            LockTypeCommand,
            AutoFitCommand,
            ExitStackCommand,
            UpgradeCommand,
            TransferAllCommand);
    }

    public override string TabHeader => Tr("City");

    public UnitGridViewModel StackGrid { get; }
    public UnitGridViewModel GarrisonGrid { get; }
    public InventoryTransferViewModel InventoryTransfer { get; }
    public DragDropViewModel DragDrop { get; }
    public DropZoneViewModel SpellDropZone { get; } = new();
    public DropZoneViewModel DismissDropZone { get; } = new();
    public IReadOnlyList<DropZoneViewModel> DropZones { get; }
    public LeaderBlankslateViewModel LeaderBlankslate { get; }

    [ObservableProperty]
    public partial string StackGridTitle { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string StackLeadership { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string GarrisonCapacity { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasStack { get; private set; }

    [ObservableProperty]
    public partial StackLeaderViewModel? Leader { get; private set; }

    [ObservableProperty]
    public partial string CityTitle { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial TextSpans CitySummary { get; private set; } = TextSpans.Empty;

    [ObservableProperty]
    public partial TextSpans GarrisonSummary { get; private set; } = TextSpans.Empty;

    [ObservableProperty]
    public partial AssetSpriteKey? CityImageKey { get; private set; }

    [ObservableProperty]
    public partial CapitalSceneViewModel? CapitalScene { get; private set; }

    [ObservableProperty]
    public partial string UpgradeLabel { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial ResourceSetViewModel? UpgradeCost { get; private set; }

    [ObservableProperty]
    public partial UnitActionType? TargetedAction { get; private set; }

    void IInventoryTransferProvider.OnItemTransferred()
    {
        RefreshView();
    }

    async Task<bool> IInventoryTransferProvider.TransferItemAsync(WorldItemId itemId, InventoryTransferDirection direction)
    {
        WorldTransferInventoryItemCommand? command = BuildTransferCommand(itemId, direction);

        if (command is null)
        {
            return false;
        }

        return await TryDispatchWorldActionAsync(command);
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

            case (InventorySlotReference sourceSlot, UnitGridSlotReference targetSlot):
                await UseItemAsync(sourceSlot, targetSlot);
                break;

            case (InventorySlotReference sourceSlot, DropZoneReference zone) when zone.Zone == SpellDropZone:
                await UseSpellItemAsync(sourceSlot);
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

    private WorldTransferInventoryItemCommand? BuildTransferCommand(WorldItemId itemId, InventoryTransferDirection direction)
    {
        if (visitingStack is null)
        {
            return null;
        }

        WorldUnitGroup stackTarget = new WorldUnitGroup.Stack(visitingStack.Id);
        WorldUnitGroup fortificationTarget = new WorldUnitGroup.Fortification(fortificationId);

        return direction switch
        {
            InventoryTransferDirection.ToOther => new WorldTransferInventoryItemCommand(stackTarget, fortificationTarget, itemId),
            InventoryTransferDirection.FromOther => new WorldTransferInventoryItemCommand(fortificationTarget, stackTarget, itemId)
        };
    }

    private WorldTransferInventoryCommand? BuildTransferAllCommand(InventoryTransferDirection direction)
    {
        if (visitingStack is null)
        {
            return null;
        }

        WorldUnitGroup stackTarget = new WorldUnitGroup.Stack(visitingStack.Id);
        WorldUnitGroup fortificationTarget = new WorldUnitGroup.Fortification(fortificationId);

        return direction switch
        {
            InventoryTransferDirection.ToOther => new WorldTransferInventoryCommand(stackTarget, fortificationTarget),
            InventoryTransferDirection.FromOther => new WorldTransferInventoryCommand(fortificationTarget, stackTarget)
        };
    }

    private WorldItem? GetStackItem(InventorySlotReference slot)
    {
        if (slot.Inventory == InventoryTransfer.Inventory)
        {
            return slot.Inventory.GetItem(slot.Index);
        }
        else
        {
            return null;
        }
    }

    private async Task UseItemAsync(InventorySlotReference sourceSlot, UnitGridSlotReference targetSlot)
    {
        WorldItem? item = GetStackItem(sourceSlot);
        WorldUnit? unit = FindUnit(targetSlot);

        if (visitingStack is not null && item is not null && unit is not null)
        {
            CityUnitGridType gridType = GetGridType(targetSlot.Grid);

            if (gridType == CityUnitGridType.Stack)
            {
                WorldItemTarget.Unit target = new(unit.Id);

                if (await TryDispatchWorldActionAsync(new WorldUseItemCommand(visitingStack.Id, item.Id, target)))
                {
                    ItemDefinition definition = GameModel.Definitions.GetItem(item.DefinitionId);

                    Game.PlaySound(ItemSoundResolver.ResolveWorld(definition.Category));
                }

                RefreshView();
            }
        }
    }

    async Task IUnitGridSelectionProvider.InvokeEmptySlotAsync(UnitGridSlotReference target)
    {
        if (GetGridType(target.Grid) == CityUnitGridType.Garrison && fortification is not null)
        {
            Game.OpenScreen(new UnitPurchaseViewModel(GameModel, Game, fortificationId, target.Position));
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

    async Task<bool> IUnitGridSelectionProvider.MoveUnitAsync(UnitGridSlotReference source, UnitGridSlotReference target)
    {
        IWorldCommand<Empty>? moveCommand = BuildMoveCommand(source, target);

        if (moveCommand is not null)
        {
            return await TryDispatchWorldActionAsync(moveCommand);
        }
        else
        {
            return false;
        }
    }

    private CityUnitGridType GetGridType(UnitGridViewModel grid)
    {
        if (grid == StackGrid)
        {
            return CityUnitGridType.Stack;
        }
        else
        {
            return CityUnitGridType.Garrison;
        }
    }

    [RelayCommand(CanExecute = nameof(CanTransferAll))]
    private async Task TransferAllAsync(InventoryTransferDirection direction)
    {
        WorldTransferInventoryCommand? command = BuildTransferAllCommand(direction);

        if (command is not null)
        {
            await TryDispatchWorldActionAsync(command);
        }
    }

    private bool CanTransferAll(InventoryTransferDirection direction)
    {
        if (IsBusy)
        {
            return false;
        }

        WorldTransferInventoryCommand? command = BuildTransferAllCommand(direction);

        if (command is not null)
        {
            WorldCommandResult commandResult = GameModel.World.DryRunCommand(command);

            return !commandResult.Failed;
        }
        else
        {
            return false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAutoFit))]
    private async Task AutoFitAsync()
    {
        if (visitingStack is not null)
        {
            WorldAutoFitStackCommand command = new(visitingStack.Id);

            await TryDispatchWorldActionAsync(command);
        }
    }

    private bool CanAutoFit()
    {
        if (IsBusy)
        {
            return false;
        }

        return visitingStack is not null;
    }

    [RelayCommand(CanExecute = nameof(CanExitStack))]
    private async Task ExitStackAsync()
    {
        if (visitingStack is not null)
        {
            WorldExitStackCommand command = new(visitingStack.Id);

            if (await TryDispatchWorldActionAsync(command))
            {
                screen.Close();
            }
        }
    }

    private bool CanExitStack()
    {
        if (IsBusy)
        {
            return false;
        }

        return visitingStack is not null;
    }

    [RelayCommand(CanExecute = nameof(CanTargetUnitAction))]
    private void Heal()
    {
        BeginTargeting(UnitActionType.Heal);
    }

    [RelayCommand(CanExecute = nameof(CanTargetUnitAction))]
    private void Resurrect()
    {
        BeginTargeting(UnitActionType.Resurrect);
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
        WorldCommandError? error = GetActionError(type, cityInfo);

        if (error is not null)
        {
            Game.ShowError(WorldCommandErrorFormatter.Format(error.Value));
        }
        else if (!HasActionTarget(type))
        {
            IconMoniker icon = type switch
            {
                UnitActionType.Heal => IconMoniker.Heal,
                UnitActionType.Resurrect => IconMoniker.Resurrect,
                UnitActionType.LockType => IconMoniker.Locked
            };

            string caption = type switch
            {
                UnitActionType.Heal => Tr("No units can be healed"),
                UnitActionType.Resurrect => Tr("No units can be resurrected"),
                UnitActionType.LockType => Tr("No units can have their type locked")
            };

            Game.ShowInfo(icon, caption, null);
        }
        else
        {
            TargetedAction = type;
            RefreshView();
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

    private bool HasActionTarget(UnitActionType type)
    {
        foreach (WorldUnitActionInfo unit in stackInfo.Units.Concat(garrisonInfo.Units))
        {
            if (GetActionError(type, unit) is null)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsActionTarget(UnitActionType type, CityUnitGridType gridType, WorldUnitId unitId)
    {
        WorldUnitActionInfo? unitInfo = FindUnitInfo(gridType, unitId);

        return unitInfo is not null && GetActionError(type, unitInfo) is null;
    }

    private static WorldCommandError? GetActionError(UnitActionType type, WorldGetCityInfoCommand.Result city)
    {
        return type switch
        {
            UnitActionType.Heal => city.HealError,
            UnitActionType.Resurrect => city.HealError,
            UnitActionType.LockType => null
        };
    }

    private static WorldCommandError? GetActionError(UnitActionType type, WorldUnitActionInfo unit)
    {
        return type switch
        {
            UnitActionType.Heal => unit.HealError,
            UnitActionType.Resurrect => unit.ResurrectError,
            UnitActionType.LockType => unit.LockTypeError
        };
    }

    async Task<bool> IUnitGridSelectionProvider.TryInvokeUnitSlotAsync(UnitGridSlotReference target)
    {
        if (TargetedAction is not null)
        {
            await ApplyTargetedActionAsync(TargetedAction.Value, target);

            return true;
        }
        else
        {
            return false;
        }
    }

    private async Task ApplyTargetedActionAsync(UnitActionType type, UnitGridSlotReference target)
    {
        WorldUnitGroup? group = ResolveGroup(GetGridType(target.Grid));
        WorldUnit? unit = FindUnit(target);

        if (group is not null && unit is not null)
        {
            Task action = type switch
            {
                UnitActionType.Heal => HealUnitAsync(group, unit),
                UnitActionType.Resurrect => ResurrectUnitAsync(group, unit),
                UnitActionType.LockType => ConfirmLockTypeAsync(group, unit)
            };

            await action;
        }
    }

    private async Task HealUnitAsync(WorldUnitGroup group, WorldUnit unit)
    {
        if (await TryDispatchWorldActionAsync(new WorldHealUnitCommand(group, unit.Id)))
        {
            AssetSoundKey.BattleEffect.Potion soundKey = new(AssetSoundKey.BattleEffect.PotionType.Heal);

            Game.PlaySound(soundKey);
        }
    }

    private async Task ResurrectUnitAsync(WorldUnitGroup group, WorldUnit unit)
    {
        if (await TryDispatchWorldActionAsync(new WorldResurrectUnitCommand(group, unit.Id)))
        {
            AssetSoundKey.BattleEffect.Potion soundKey = new(AssetSoundKey.BattleEffect.PotionType.Revive);

            Game.PlaySound(soundKey);
        }
    }

    private async Task ConfirmLockTypeAsync(WorldUnitGroup group, WorldUnit unit)
    {
        string unitName = UnitNameResolver.Resolve(unit, GameModel.Definitions, GameModel.Texts);
        WorldLockUnitTypeCommand command = new(group, unit.Id);

        Game.OpenScreen(new ConfirmPromptViewModel(
            Tr("Lock unit type"),
            TrSpans("Lock the type of {0:*}? It will never upgrade to another type and will gain levels instead. This cannot be undone.", unitName),
            Tr("Lock"),
            ConfirmPromptType.Danger,
            async () =>
            {
                await TryDispatchWorldActionAsync(command);
                RefreshView();
            }));
    }

    private void ConfirmDisband(UnitGridSlotReference slot)
    {
        WorldUnit? unit = FindUnit(slot);
        WorldDisbandUnitCommand? command = BuildDisbandCommand(slot);

        if (unit is not null && command is not null)
        {
            Game.OpenScreen(new ConfirmPromptViewModel(
                Tr("Dismiss unit"),
                TrSpans("Are you sure you want to dismiss {0:*}?", UnitNameResolver.Resolve(unit, GameModel.Definitions, GameModel.Texts)),
                Tr("Dismiss"),
                ConfirmPromptType.Danger,
                async () =>
                {
                    if (await TryDispatchWorldActionAsync(command))
                    {
                        unitSelection.ClearSelection();
                    }

                    RefreshView();
                }));
        }
    }

    private bool CanDisband(UnitGridSlotReference slot)
    {
        WorldUnit? unit = FindUnit(slot);

        if (unit is not null)
        {
            WorldUnitActionInfo? unitInfo = FindUnitInfo(GetGridType(slot.Grid), unit.Id);

            return unitInfo is not null && unitInfo.DisbandError is null;
        }
        else
        {
            return false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanUpgrade))]
    private void Upgrade()
    {
        if (fortification is WorldCity city)
        {
            WorldCityDefinition definition = GameModel.Definitions.GetCity(city.DefinitionId);

            Game.OpenScreen(new ConfirmPromptViewModel(
                Tr("Upgrade city"),
                TrSpans("Upgrade the city to {0:*}? This will increase garrison capacity, regeneration and armor bonus.", Tr("level {0}", definition.Level + 1)),
                Tr("Upgrade"),
                UpgradeCost,
                ConfirmPromptType.Primary,
                async () =>
                {
                    if (await TryDispatchWorldActionAsync(new WorldUpgradeCityCommand(city.Id)))
                    {
                        Game.PlaySound(new AssetSoundKey.CityGrow());
                    }

                    RefreshView();
                }));
        }
    }

    private bool CanUpgrade()
    {
        if (IsBusy)
        {
            return false;
        }

        return cityInfo.Growth is not null && cityInfo.Growth.Error is null;
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

    private IReadOnlySet<GridPosition> BuildUnitHighlight(UnitGridViewModel grid, UnitGrid<WorldUnit> units, IDragSourceReference? reference)
    {
        return reference switch
        {
            UnitGridSlotReference source => BuildMoveHighlight(source, grid),
            InventorySlotReference source when grid == StackGrid => BuildUseItemHighlight(GetStackItem(source)),
            null when TargetedAction is not null => BuildTargetHighlight(TargetedAction.Value, grid, units),
            _ => new HashSet<GridPosition>()
        };
    }

    private IReadOnlySet<GridPosition> BuildTargetHighlight(UnitActionType type, UnitGridViewModel grid, UnitGrid<WorldUnit> units)
    {
        HashSet<GridPosition> positions = [];
        CityUnitGridType gridType = GetGridType(grid);

        foreach ((WorldUnit unit, GridPosition position) in units.GetUnits())
        {
            if (IsActionTarget(type, gridType, unit.Id))
            {
                positions.Add(position);
            }
        }

        return positions;
    }

    private IReadOnlySet<GridPosition> BuildMoveHighlight(UnitGridSlotReference source, UnitGridViewModel grid)
    {
        HashSet<GridPosition> positions = [];

        foreach (GridPosition position in GridPosition.All)
        {
            UnitGridSlotReference target = new(grid, position);
            IWorldCommand<Empty>? moveCommand = BuildMoveCommand(source, target);

            if (target != source && moveCommand is not null)
            {
                WorldCommandResult commandResult = GameModel.World.DryRunCommand(moveCommand);

                if (!commandResult.Failed)
                {
                    positions.Add(position);
                }
            }
        }

        return positions;
    }

    private IReadOnlySet<GridPosition> BuildUseItemHighlight(WorldItem? item)
    {
        HashSet<GridPosition> positions = [];

        if (visitingStack is null || item is null)
        {
            return positions;
        }

        foreach ((WorldUnit unit, GridPosition position) in visitingStack.Units.GetUnits())
        {
            WorldUseItemCommand command = new(visitingStack.Id, item.Id, new WorldItemTarget.Unit(unit.Id));
            WorldCommandResult commandResult = GameModel.World.DryRunCommand(command);

            if (!commandResult.Failed)
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

        if (item is null || direction is null)
        {
            return null;
        }

        WorldTransferInventoryItemCommand? command = BuildTransferCommand(item.Id, direction.Value);
        if (command is null)
        {
            return null;
        }

        WorldCommandResult commandResult = GameModel.World.DryRunCommand(command);
        if (commandResult.Failed)
        {
            return null;
        }
        else
        {
            return direction;
        }
    }

    private void UpdateDismissDropZone(IDragSourceReference? dragged)
    {
        WorldUnit? unit = null;

        if (dragged is UnitGridSlotReference source && CanDisband(source))
        {
            unit = FindUnit(source);
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

    private void UpdateSpellDropZone(IDragSourceReference? dragged)
    {
        WorldItem? item = null;

        if (dragged is InventorySlotReference source)
        {
            item = GetStackItem(source);
        }

        if (visitingStack is null || item is null)
        {
            SpellDropZone.Hide();

            return;
        }

        ItemDefinition definition = GameModel.Definitions.GetItem(item.DefinitionId);

        if (definition.Effect is SpellItemEffect)
        {
            SpellDropZone.Show(Tr("Use {0}", GameModel.Texts.GetGlobalText(definition.NameTextId).Text));
        }
        else
        {
            SpellDropZone.Hide();
        }
    }

    private IWorldCommand<Empty>? BuildMoveCommand(UnitGridSlotReference source, UnitGridSlotReference target)
    {
        WorldUnit? unit = FindUnit(source);

        if (unit is null)
        {
            return null;
        }

        CityUnitGridType sourceType = GetGridType(source.Grid);
        CityUnitGridType targetType = GetGridType(target.Grid);

        if (sourceType == targetType)
        {
            WorldUnitGroup? group = ResolveGroup(sourceType);

            if (group is not null)
            {
                return new WorldRepositionUnitCommand(group, unit.Id, target.Position);
            }
            else
            {
                return null;
            }
        }
        else if (visitingStack is not null)
        {
            WorldUnitGroup stackTarget = new WorldUnitGroup.Stack(visitingStack.Id);
            WorldUnitGroup fortificationTarget = new WorldUnitGroup.Fortification(fortificationId);

            WorldTransferUnitCommand command;
            if (sourceType == CityUnitGridType.Stack)
            {
                command = new WorldTransferUnitCommand(stackTarget, fortificationTarget, unit.Id, target.Position);
            }
            else
            {
                command = new WorldTransferUnitCommand(fortificationTarget, stackTarget, unit.Id, target.Position);
            }

            return command;
        }
        else
        {
            return null;
        }
    }

    private void RefreshView()
    {
        fortification = ResolveFortification();

        if (fortification is null)
        {
            visitingStack = null;
            screen.Close();

            return;
        }

        visitingStack = ResolveVisitingStack(fortification);
        stackInfo = ResolveStackInfo(visitingStack);
        garrisonInfo = ResolveGarrisonInfo();
        cityInfo = ResolveCityInfo();

        UnitGrid<WorldUnit> garrison = fortification.Garrison;
        UnitGrid<WorldUnit> stackUnits = visitingStack?.Units ?? UnitGrid<WorldUnit>.Empty;
        WorldInventory stackInventory = visitingStack?.Inventory ?? WorldInventory.Empty;
        IDragSourceReference? dragged = DragDrop.Dragged;

        if (visitingStack is not null)
        {
            StackLeaderViewModel leader = StackLeaderViewModel.Create(
                visitingStack, WorldState.Players[visitingStack.OwnerId], GameModel.Definitions, GameModel.Texts, GameModel.Stats);

            Leader = leader;
            StackGridTitle = Tr("{0}'s party", leader.Title);
            HasStack = true;
        }
        else
        {
            Leader = null;
            StackGridTitle = string.Empty;
            HasStack = false;
        }

        WorldPlayer player = WorldState.Players[ViewPlayerId];
        InventoryTransfer.Update(stackInventory, fortification.Inventory, player.Resources, BuildTransferHighlight(dragged));

        StackLeadership = FormatStackLeadership(stackInfo);
        GarrisonCapacity = FormatGarrisonCapacity(garrisonInfo);
        StackGrid.Update(stackUnits, showsPurchaseHints: false, BuildUnitHighlight(StackGrid, stackUnits, dragged));
        GarrisonGrid.Update(garrison, showsPurchaseHints: true, BuildUnitHighlight(GarrisonGrid, garrison, dragged));

        CityTitle = GameModel.Texts.GetScenarioText(fortification.NameTextId);
        CitySummary = FormatCitySummary(fortification);
        GarrisonSummary = FormatGarrisonSummary(garrison);
        CityImageKey = ResolveCityImageKey(fortification);
        CapitalScene = BuildCapitalScene(fortification);
        UpgradeLabel = FormatUpgradeLabel(cityInfo.Growth);
        UpgradeCost = ResolveUpgradeCost(cityInfo.Growth);

        UpgradeCommand.NotifyCanExecuteChanged();
        TransferAllCommand.NotifyCanExecuteChanged();

        UpdateSpellDropZone(dragged);
        UpdateDismissDropZone(dragged);
    }

    private async Task UseSpellItemAsync(InventorySlotReference sourceSlot)
    {
        WorldItem? item = GetStackItem(sourceSlot);

        if (visitingStack is null || item is null)
        {
            return;
        }

        ItemDefinition definition = GameModel.Definitions.GetItem(item.DefinitionId);

        if (definition.Effect is not SpellItemEffect effect)
        {
            return;
        }

        WorldSpellDefinition spell = GameModel.Definitions.GetSpell(effect.SpellId);

        if (spell.AreaRadius is null)
        {
            if (await TryDispatchWorldActionAsync(new WorldUseItemCommand(visitingStack.Id, item.Id, Target: null)))
            {
                Game.PlaySound(ItemSoundResolver.ResolveWorld(definition.Category));
                screen.Close();
            }
            else
            {
                RefreshView();
            }
        }
        else if (Game.RootScreen is WorldScreenViewModel world)
        {
            screen.Close();
            world.SelectAction(new WorldCursorAction.UseItem(visitingStack.Id, item.Id));
        }
    }

    private WorldFortification? ResolveFortification()
    {
        if (WorldState.Fortifications.TryGetValue(fortificationId, out WorldFortification? resolved) &&
            resolved is (WorldCity or WorldCapital) &&
            resolved.Owner == ViewPlayerId)
        {
            return resolved;
        }
        else
        {
            return null;
        }
    }

    private WorldStack? ResolveVisitingStack(WorldFortification fortification)
    {
        WorldPlan plan = WorldPlan.For(WorldState);
        WorldStack? stack = plan.GetStackAt(fortification.Entrance);

        if (stack is not null && stack.OwnerId == ViewPlayerId)
        {
            return stack;
        }
        else
        {
            return null;
        }
    }

    private static string FormatStackLeadership(WorldGetStackInfoCommand.Result info)
    {
        return $"{info.OccupiedLeadership} / {info.TotalLeadership}";
    }

    private static string FormatGarrisonCapacity(WorldGetGarrisonInfoCommand.Result info)
    {
        return $"{info.OccupiedCapacity} / {info.TotalCapacity}";
    }

    private TextSpans FormatCitySummary(WorldFortification fortification)
    {
        if (fortification is WorldCity city)
        {
            WorldCityDefinition definition = GameModel.Definitions.GetCity(city.DefinitionId);

            return TrSpans("City level: {0:*}", definition.Level);
        }
        else
        {
            return TrSpans("Capital");
        }
    }

    private static TextSpans FormatGarrisonSummary(UnitGrid<WorldUnit> garrison)
    {
        return TrSpans("Units in city: {0:*}", garrison.GetUnits().Count());
    }

    private WorldGetCityInfoCommand.Result ResolveCityInfo()
    {
        ICommandResult<WorldCommandError, WorldEvent, WorldGetCityInfoCommand.Result> result =
            GameModel.World.DryRunCommand(new WorldGetCityInfoCommand(fortificationId));

        if (result.Failed)
        {
            return new WorldGetCityInfoCommand.Result(Growth: null, HealError: null);
        }
        else
        {
            return result.Data;
        }
    }

    private WorldGetStackInfoCommand.Result ResolveStackInfo(WorldStack? stack)
    {
        if (stack is not null)
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
        else
        {
            return new WorldGetStackInfoCommand.Result([], OccupiedLeadership: 0, TotalLeadership: 0);
        }
    }

    private WorldGetGarrisonInfoCommand.Result ResolveGarrisonInfo()
    {
        ICommandResult<WorldCommandError, WorldEvent, WorldGetGarrisonInfoCommand.Result> result =
            GameModel.World.DryRunCommand(new WorldGetGarrisonInfoCommand(fortificationId));

        if (result.Failed)
        {
            return new WorldGetGarrisonInfoCommand.Result([], OccupiedCapacity: 0, TotalCapacity: 0);
        }
        else
        {
            return result.Data;
        }
    }

    private WorldUnitActionInfo? FindUnitInfo(CityUnitGridType gridType, WorldUnitId unitId)
    {
        ImmutableArray<WorldUnitActionInfo> units = gridType switch
        {
            CityUnitGridType.Stack => stackInfo.Units,
            CityUnitGridType.Garrison => garrisonInfo.Units
        };

        foreach (WorldUnitActionInfo unit in units)
        {
            if (unit.UnitId == unitId)
            {
                return unit;
            }
        }

        return null;
    }

    private static string FormatUpgradeLabel(WorldGetCityInfoCommand.Growth? growth)
    {
        if (growth is null)
        {
            return string.Empty;
        }
        else if (growth.Error == WorldCommandError.CityFullyUpgraded)
        {
            return Tr("City fully upgraded");
        }
        else
        {
            return Tr("Upgrade city");
        }
    }

    private static ResourceSetViewModel? ResolveUpgradeCost(WorldGetCityInfoCommand.Growth? growth)
    {
        if (growth is null || growth.Error == WorldCommandError.CityFullyUpgraded)
        {
            return null;
        }
        else
        {
            return new ResourceSetViewModel(growth.Cost, ResourceSetProjection.All);
        }
    }

    private WorldUnit? FindUnit(UnitGridSlotReference slot)
    {
        CityUnitGridType type = GetGridType(slot.Grid);

        if (type == CityUnitGridType.Stack)
        {
            return visitingStack?.Units[slot.Position];
        }
        else
        {
            return fortification?.Garrison[slot.Position];
        }
    }

    private WorldUnitGroup? ResolveGroup(CityUnitGridType type)
    {
        if (type == CityUnitGridType.Stack)
        {
            if (visitingStack is not null)
            {
                return new WorldUnitGroup.Stack(visitingStack.Id);
            }
            else
            {
                return null;
            }
        }
        else
        {
            return new WorldUnitGroup.Fortification(fortificationId);
        }
    }

    private WorldDisbandUnitCommand? BuildDisbandCommand(UnitGridSlotReference slot)
    {
        WorldUnitGroup? group = ResolveGroup(GetGridType(slot.Grid));
        WorldUnit? unit = FindUnit(slot);

        if (group is not null && unit is not null)
        {
            return new WorldDisbandUnitCommand(group, unit.Id);
        }
        else
        {
            return null;
        }
    }

    private AssetSpriteKey? ResolveCityImageKey(WorldFortification fortification)
    {
        return fortification switch
        {
            WorldCapital capital => new AssetSpriteKey.CapitalImage(WorldState.Players[capital.OwnerId].Race),
            WorldCity city => new AssetSpriteKey.CityImage(city.DefinitionId.Level),
            _ => null
        };
    }

    private CapitalSceneViewModel? BuildCapitalScene(WorldFortification fortification)
    {
        if (fortification is WorldCapital capital)
        {
            WorldRace race = WorldState.Players[capital.OwnerId].Race;
            WorldPlayerBuildings buildings = WorldState.Players[capital.OwnerId].Buildings;

            return CapitalSceneViewModel.Create(race, buildings.Acquired, constructedBuildingId: null);
        }
        else
        {
            return null;
        }
    }
}
