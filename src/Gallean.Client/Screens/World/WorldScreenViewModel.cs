using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Common;
using Gallean.Engine;
using static Gallean.Client.WorldEventQueue;

namespace Gallean.Client;

internal sealed partial class WorldScreenViewModel :
    AbstractGameScreenViewModel,
    IWorldPathEncounterVisitor<WorldScreenViewModel.SelectedMove, Task>,
    IWorldStackActionVisitor<Task>
{
    internal sealed record SelectedMove(WorldStack Stack, WorldPathSelection Selection);

    private static readonly TimeSpan StackIdleDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan StackIdleInterval = TimeSpan.FromMinutes(2);

    private readonly CursorTypeVisitor cursorTypeVisitor;
    private readonly EventActionPlayer eventActionPlayer;
    private readonly WorldEventQueue eventQueue;
    private readonly WorldFinishedStacks finishedStacks;
    private readonly MusicQueue musicQueue;
    private readonly IRandomProvider random;
    private WorldCursor cursor = WorldCursor.Empty;
    private WorldPosition? infoTile;
    private IReadOnlyList<WorldPosition> infoObjects = [];
    private WorldStackId? infoStackId;
    private TimeSpan stackIdleRemaining;
    private WorldRenderRequest? renderRequest;

    public WorldScreenViewModel(GameModel gameModel, GameViewModel gameViewModel, IRandomProvider random)
        : base(gameModel, gameViewModel)
    {
        this.random = random;

        cursorTypeVisitor = new CursorTypeVisitor(gameModel);
        eventActionPlayer = new EventActionPlayer(this);

        eventQueue = new WorldEventQueue(gameModel.Scenario, gameModel.World.Events);
        eventQueue.EnqueueEntry(new Entry.GameStart(WorldState));
        finishedStacks = new WorldFinishedStacks(gameModel.World.Events);
        musicQueue = BuildWorldMusicQueue(random);

        UpdateWorld();
        UpdateObjective();
        FocusCamera(ResolveInitialFocusPosition(), TimeSpan.Zero);
        RestartStackIdleTimer();

        Game.PropertyChanged += OnGamePropertyChanged;
        Game.DevTools.PropertyChanged += OnDevToolsPropertyChanged;
        RefreshRenderRequest();

        RegisterSnapshotGatedCommands(
            SelectStackItemCommand,
            EndTurnCommand,
            ActivateStackActionCommand,
            InspectStackCommand,
            FinishStackCommand,
            SelectPreviousStackCommand,
            SelectNextStackCommand,
            OpenCapitalCommand,
            OpenSpellbookCommand,
            OpenStackCommand);
    }

    [ObservableProperty]
    public partial WorldPosition? CameraFocus { get; private set; }

    [ObservableProperty]
    public partial WorldStatusViewModel Status { get; private set; }

    [ObservableProperty]
    public partial WorldPositionInfoViewModel? PositionInfo { get; private set; }

    [ObservableProperty]
    public partial string? ActionPromptText { get; private set; }

    [ObservableProperty]
    public partial string? ObjectiveText { get; private set; }

    [ObservableProperty]
    public partial bool IsObjectiveExpanded { get; private set; } = true;

    [ObservableProperty]
    public partial bool IsEndTurnHighlighted { get; private set; }

    [ObservableProperty]
    public partial bool IsViewPlayerTurn { get; private set; }

    public ObservableCollection<WorldStackItemViewModel> StackItems { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FastForwardCommand))]
    public partial bool IsFastForward { get; private set; }

    [ObservableProperty]
    public partial WorldActionWheelViewModel? ActionWheel { get; private set; }

    [ObservableProperty]
    public partial bool IsActionWheelVisible { get; private set; }

    [ObservableProperty]
    public partial bool IsInBattle { get; private set; }

    public WorldRenderRequest RenderRequest
    {
        get
        {
            Debug.Assert(renderRequest is not null);
            return renderRequest;
        }
        private set => SetRenderRequest(value);
    }

    public bool IsActive => ReferenceEquals(Game.TopScreen, this);
    public int MapWidth => WorldState.Map.Width;
    public int MapHeight => WorldState.Map.Height;

    public override bool PaintsOwnBackground => true;

    public override ScreenMusic Music => ScreenMusic.Tracks(musicQueue);

    protected override bool IsBusy => base.IsBusy || !Snapshot.IsViewPlayerTurn || !eventQueue.IsIdle;

    private bool FogEnabled => Game.DevTools.IsFogEnabled;

    public event Action<WorldPosition, TimeSpan>? CameraFocusRequested;
    public event Action? ObjectiveChanged;

    [RelayCommand(CanExecute = nameof(CanFastForward))]
    private void FastForward()
    {
        IsFastForward = true;
    }

    private bool CanFastForward()
    {
        return IsActive &&
               !IsFastForward &&
               renderRequest is not null &&
               renderRequest.State.TurnInfo.CurrentPlayerId != ViewPlayerId;
    }

    public void HandleRenderException(Exception ex)
    {
        GameModel.Log.Error("Failed to render world.", ex);

        Game.ShowScreen(new ErrorScreenViewModel(
            Tr("Failed to render world.\n{0}", ex.Message),
            Tr("Exit"),
            () => Game.ExitCommand.Execute(null)));
    }

    protected override void OnDispose()
    {
        Game.PropertyChanged -= OnGamePropertyChanged;
        Game.DevTools.PropertyChanged -= OnDevToolsPropertyChanged;
        eventQueue.Dispose();
        finishedStacks.Dispose();
    }

    [RelayCommand]
    private void OpenGameMenu()
    {
        Game.OpenScreen(new GameMenuViewModel(GameModel, Game));
    }

    [RelayCommand]
    private void ToggleObjective()
    {
        IsObjectiveExpanded = !IsObjectiveExpanded;
    }

    public async Task HandleMapClickAsync(
        WorldPosition tile,
        IReadOnlyList<WorldPosition> objectPositions,
        int clickCount)
    {
        if (!WorldState.Map.IsValidPosition(tile))
        {
            return;
        }

        if (IsBusy)
        {
            return;
        }

        if (cursor.Action is not null)
        {
            await DispatchSelectedActionAsync(cursor.Action, tile);
            return;
        }

        if (TryHandleInspectAction(tile, objectPositions, clickCount))
        {
            return;
        }

        WorldPosition position = ResolveClickTarget(ResolvePointerTile(tile, objectPositions));

        if (TryHandleStackAction(position))
        {
            return;
        }

        if (TryHandleFortificationBodyAction(position))
        {
            return;
        }

        if (await TryHandleMoveActionAsync(position))
        {
            return;
        }

        if (TryHandleFortificationAction(position))
        {
            return;
        }

        SelectStackAt(position);
    }

    private bool TryHandleInspectAction(WorldPosition tile, IReadOnlyList<WorldPosition> objectPositions, int clickCount)
    {
        WorldStack? selected = ResolveSelectedStack();

        if (clickCount == 2 && selected is not null)
        {
            WorldPlan plan = WorldPlan.For(WorldState);
            WorldVisibility visibility = WorldVisibility.Create(WorldState, ViewPlayerId, FogEnabled);
            WorldFortification? fortification = visibility.GetFortificationAt(plan, selected.Position);

            if (fortification is not null && fortification.Owner == ViewPlayerId)
            {
                return false;
            }

            if (selected.Position == tile || objectPositions.Contains(selected.Position))
            {
                InspectStack();

                return true;
            }
        }

        return false;
    }

    private bool TryHandleFortificationBodyAction(WorldPosition position)
    {
        WorldFortification? fortification = ResolveFortificationBodyAt(position);

        if (fortification is not null && fortification.Owner == ViewPlayerId)
        {
            OpenFortification(fortification);

            return true;
        }
        else
        {
            return false;
        }
    }

    private bool TryHandleStackAction(WorldPosition position)
    {
        WorldPlan plan = WorldPlan.For(WorldState);
        WorldVisibility visibility = WorldVisibility.Create(WorldState, ViewPlayerId, FogEnabled);
        WorldStack? stack = visibility.GetStackAt(plan, position, ViewPlayerId);

        if (stack is null || stack.OwnerId != ViewPlayerId)
        {
            return false;
        }

        if (cursor.StackId != stack.Id)
        {
            PlayStackResponseSound(stack);
            SelectStack(stack.Id);
        }
        else
        {
            WorldFortification? fortification = visibility.GetFortificationAt(plan, stack.Position);

            if (fortification is not null && fortification.Owner == ViewPlayerId)
            {
                OpenFortification(fortification);
            }
        }

        return true;
    }

    private async Task<bool> TryHandleMoveActionAsync(WorldPosition position)
    {
        WorldStack? selected = ResolveSelectedStack();

        if (selected is null || position == selected.Position)
        {
            return false;
        }

        if (cursor.ReachablePositions.Contains(position))
        {
            WorldPathSelection? path = BuildPathSelection(selected, position);

            if (path is not null)
            {
                await MoveSelectedStackAsync(selected, path);
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    private bool TryHandleFortificationAction(WorldPosition position)
    {
        WorldPlan plan = WorldPlan.For(WorldState);
        WorldVisibility visibility = WorldVisibility.Create(WorldState, ViewPlayerId, FogEnabled);
        WorldFortification? fortification = visibility.GetFortificationAt(plan, position);

        if (fortification is null || fortification.Owner != ViewPlayerId)
        {
            return false;
        }

        OpenFortification(fortification);

        return true;
    }

    private void SelectStackAt(WorldPosition position)
    {
        WorldPlan plan = WorldPlan.For(WorldState);
        WorldVisibility visibility = WorldVisibility.Create(WorldState, ViewPlayerId, FogEnabled);
        WorldStack? stack = visibility.GetStackAt(plan, position, ViewPlayerId);

        if (stack is not null && stack.OwnerId == ViewPlayerId)
        {
            SelectStack(stack.Id);
        }
        else
        {
            SelectStack(null);
        }
    }

    public void ShowPositionInfo(WorldPosition tile, IReadOnlyList<WorldPosition> objectPositions)
    {
        infoTile = tile;
        infoObjects = objectPositions;
        infoStackId = null;
        UpdatePositionInfo();
    }

    public void ShowStackInfo(WorldStackId stackId)
    {
        infoTile = null;
        infoObjects = [];
        infoStackId = stackId;
        UpdatePositionInfo();
    }

    public void HidePositionInfo()
    {
        infoTile = null;
        infoObjects = [];
        infoStackId = null;
        UpdatePositionInfo();
    }

    private void UpdatePositionInfo()
    {
        if (infoStackId is not null)
        {
            PositionInfo = CreateStackInfo(infoStackId.Value);
        }
        else if (infoTile is not null)
        {
            WorldPlan plan = WorldPlan.For(WorldState);
            WorldVisibility visibility = WorldVisibility.Create(WorldState, ViewPlayerId, FogEnabled);
            WorldPositionInfoViewModel? info = CreatePositionInfo(infoTile.Value, plan, visibility);

            if (info is null && infoObjects.Count > 0)
            {
                info = CreatePositionInfo(infoObjects[0], plan, visibility);
            }

            PositionInfo = info;
        }
        else
        {
            PositionInfo = null;
        }
    }

    private WorldStackInfoViewModel? CreateStackInfo(WorldStackId stackId)
    {
        if (WorldState.Stacks.TryGetValue(stackId, out WorldStack? stack))
        {
            return WorldStackInfoViewModel.Create(
                WorldState,
                ViewPlayerId,
                stack,
                GameModel.Definitions,
                GameModel.Texts,
                GameModel.Stats);
        }
        else
        {
            return null;
        }
    }

    private WorldPositionInfoViewModel? CreatePositionInfo(WorldPosition position, WorldPlan plan, WorldVisibility visibility)
    {
        if (!WorldState.Map.IsValidPosition(position))
        {
            return null;
        }

        WorldFortification? fortification = plan.GetFortificationAt(position);

        if (fortification is WorldCapital capital && visibility.IsAnyRevealed(capital.Footprint))
        {
            return WorldFortificationInfoViewModel.CreateCapital(
                WorldState,
                ViewPlayerId,
                capital,
                visibility.GetStackAt(plan, capital.Entrance, ViewPlayerId),
                GameModel.Definitions,
                GameModel.Texts,
                GameModel.Stats);
        }
        else if (fortification is WorldCity city && visibility.IsAnyRevealed(city.Footprint))
        {
            return WorldFortificationInfoViewModel.CreateCity(
                WorldState,
                ViewPlayerId,
                city,
                visibility.GetStackAt(plan, city.Entrance, ViewPlayerId),
                GameModel.Definitions,
                GameModel.Texts,
                GameModel.Stats);
        }

        WorldStack? stack = visibility.GetStackAt(plan, position, ViewPlayerId);

        if (stack is not null)
        {
            return WorldStackInfoViewModel.Create(
                WorldState,
                ViewPlayerId,
                stack,
                GameModel.Definitions,
                GameModel.Texts,
                GameModel.Stats);
        }

        return WorldTileInfoViewModel.TryCreate(
            WorldState,
            ViewPlayerId,
            position,
            visibility,
            GameModel.Definitions,
            GameModel.Texts,
            GameModel.Stats);
    }

    public void UpdateHover(WorldPosition hoverTile, IReadOnlyList<WorldPosition> objectPositions)
    {
        WorldPosition tile = ResolvePointerTile(hoverTile, objectPositions);

        if (WorldState.Map.IsValidPosition(tile))
        {
            WorldPosition? hoverTarget = IsBusy ? null : ResolveHoverTarget(tile);
            WorldPathSelection? hoverPath = ResolveHoverPath(cursor, hoverTarget);

            SetCursor(cursor with { HoverTile = tile, HoverTarget = hoverTarget, HoverPath = hoverPath, HoverObjects = objectPositions });
        }
        else
        {
            SetCursor(cursor with { HoverTile = null, HoverTarget = null, HoverPath = null, HoverObjects = objectPositions });
        }
    }

    public void ClearHover()
    {
        SetCursor(cursor with { HoverTile = null, HoverTarget = null, HoverPath = null, HoverObjects = [] });
    }

    private WorldPathSelection? ResolveHoverPath(WorldCursor current, WorldPosition? hoverTarget)
    {
        WorldStack? selected = ResolveSelectedStack();

        if (hoverTarget is not null &&
            selected is not null &&
            current.Action is null &&
            current.ReachablePositions.Contains(hoverTarget.Value))
        {
            if (current.HoverPath is not null && current.HoverPath.Destination == hoverTarget.Value)
            {
                return current.HoverPath;
            }
            else
            {
                return BuildPathSelection(selected, hoverTarget.Value);
            }
        }

        return null;
    }

    [RelayCommand(CanExecute = nameof(CanDispatchCommand))]
    private void SelectStackItem(WorldStackId stackId)
    {
        if (WorldState.Stacks.TryGetValue(stackId, out WorldStack? stack))
        {
            if (cursor.StackId != stackId)
            {
                PlayStackResponseSound(stack);
            }

            FocusStack(stack);
        }
    }

    private void FocusStack(WorldStack stack)
    {
        SelectStack(stack.Id);
        FocusCamera(stack.Position, TimeSpan.Zero);
    }

    [RelayCommand(CanExecute = nameof(CanSelectPreviousStack))]
    private void SelectPreviousStack()
    {
        int index = FindStackItemIndex(cursor.StackId);
        WorldStackItemViewModel previous;

        if (index > 0)
        {
            previous = StackItems[index - 1];
        }
        else
        {
            previous = StackItems[^1];
        }

        FocusStack(WorldState.Stacks[previous.StackId]);
    }

    private bool CanSelectPreviousStack()
    {
        if (IsBusy)
        {
            return false;
        }

        return StackItems.Count > 0;
    }

    [RelayCommand(CanExecute = nameof(CanSelectNextStack))]
    private void SelectNextStack()
    {
        int index = FindStackItemIndex(cursor.StackId);
        WorldStackItemViewModel next;

        if (index >= 0 && index < StackItems.Count - 1)
        {
            next = StackItems[index + 1];
        }
        else
        {
            next = StackItems[0];
        }

        FocusStack(WorldState.Stacks[next.StackId]);
    }

    private bool CanSelectNextStack()
    {
        if (IsBusy)
        {
            return false;
        }

        return StackItems.Count > 0;
    }

    private int FindStackItemIndex(WorldStackId? stackId)
    {
        for (int index = 0; index < StackItems.Count; index++)
        {
            if (StackItems[index].StackId == stackId)
            {
                return index;
            }
        }

        return -1;
    }

    [RelayCommand(CanExecute = nameof(CanInspectStack))]
    private void InspectStack()
    {
        WorldStack? stack = ResolveSelectedStack();

        if (stack is not null)
        {
            ClearSelectedAction();
            Game.OpenScreen(ManagementScreenViewModel.CreateForStack(GameModel, Game, stack.Id));
        }
    }

    private bool CanInspectStack()
    {
        if (IsBusy)
        {
            return false;
        }

        return ResolveSelectedStack() is not null;
    }

    [RelayCommand(CanExecute = nameof(CanFinishStack))]
    private void FinishStack()
    {
        WorldStack? stack = ResolveSelectedStack();

        if (stack is null)
        {
            return;
        }

        ClearSelectedAction();
        finishedStacks.Add(stack.Id);
        UpdateStackActions();
        UpdateEndTurnHighlighted();

        WorldStackItemViewModel? nextStack = FindNextUnfinishedStackItem(stack.Id);

        if (nextStack is not null)
        {
            FocusStack(WorldState.Stacks[nextStack.StackId]);
        }
        else
        {
            SelectStack(null);
        }
    }

    private bool CanFinishStack()
    {
        if (IsBusy)
        {
            return false;
        }

        WorldStack? stack = ResolveSelectedStack();

        return stack is not null && stack.MovementPoints > 0 && !IsFinished(stack);
    }

    private WorldStackItemViewModel? FindNextUnfinishedStackItem(WorldStackId stackId)
    {
        int index = FindStackItemIndex(stackId);

        for (int offset = 1; offset <= StackItems.Count; offset++)
        {
            WorldStackItemViewModel item = StackItems[(index + offset).Mod(StackItems.Count)];

            if (item is { MovementPoints: > 0, IsFinished: false })
            {
                return item;
            }
        }

        return null;
    }

    private bool IsFinished(WorldStack stack)
    {
        return finishedStacks.Contains(stack.Id);
    }

    [RelayCommand(CanExecute = nameof(CanOpenCapital))]
    private void OpenCapital()
    {
        WorldPlan plan = WorldPlan.For(WorldState);

        if (plan.GetCapital(ViewPlayerId) is { } capital)
        {
            Game.OpenScreen(ManagementScreenViewModel.CreateForFortification(GameModel, Game, capital.Id));
        }
    }

    private bool CanOpenCapital()
    {
        if (IsBusy)
        {
            return false;
        }

        WorldPlan plan = WorldPlan.For(WorldState);

        return plan.GetCapital(ViewPlayerId) is not null;
    }

    private void OpenFortification(WorldFortification fortification)
    {
        Game.OpenScreen(ManagementScreenViewModel.CreateForFortification(GameModel, Game, fortification.Id));
    }

    [RelayCommand(CanExecute = nameof(CanDispatchCommand))]
    private void OpenSpellbook()
    {
        ClearSelectedAction();
        Game.OpenScreen(ManagementScreenViewModel.CreateForSpellbook(GameModel, Game));
    }

    [RelayCommand(CanExecute = nameof(CanOpenStack))]
    private void OpenStack()
    {
        WorldStack? stack = ResolveSelectedStack();

        if (stack is not null)
        {
            ClearSelectedAction();
            Game.OpenScreen(ManagementScreenViewModel.CreateForStack(GameModel, Game, stack.Id));
        }
    }

    private bool CanOpenStack()
    {
        if (IsBusy)
        {
            return false;
        }

        return ResolveSelectedStack() is not null;
    }

    public void SelectAction(WorldCursorAction action)
    {
        SetCursor(cursor.SelectStack(action.SelectedStackId) with { Action = action });
        UpdateReachable();
    }

    [RelayCommand]
    private void CancelSelectedAction()
    {
        if (cursor.Action is not null)
        {
            ClearSelectedAction();
        }
    }

    protected override bool TryClose()
    {
        if (cursor.Action is not null)
        {
            ClearSelectedAction();
        }
        else if (cursor.StackId is not null)
        {
            SelectStack(null);
        }
        else
        {
            Game.OpenScreen(new GameMenuViewModel(GameModel, Game));
        }

        return true;
    }

    private void ClearSelectedAction()
    {
        SetCursor(cursor with { Action = null });
    }

    private async Task DispatchSelectedActionAsync(WorldCursorAction action, WorldPosition position)
    {
        ClearSelectedAction();

        await TryDispatchWorldActionAsync(action.CreateCommand(position));
    }

    [RelayCommand(CanExecute = nameof(CanDispatchCommand))]
    private async Task EndTurnAsync()
    {
        ClearSelectedAction();
        ClearHover();

        if (!await TryDispatchWorldActionAsync(new WorldEndTurnCommand()))
        {
            return;
        }

        await Game.ExecuteAiTurnsAsync(this);
    }

    private void OnGamePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(GameViewModel.TopScreen))
        {
            OnPropertyChanged(nameof(IsActive));
            FastForwardCommand.NotifyCanExecuteChanged();

            AbstractScreenViewModel? activeScreen = eventActionPlayer.ActiveScreen;

            if (activeScreen is not null && !activeScreen.IsOpen)
            {
                StopEntry();
            }

            ProcessEventQueue();
        }
    }

    private void OnDevToolsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DevToolsViewModel.IsFogEnabled))
        {
            RefreshRenderRequest();
        }
    }

    private void SetCursor(WorldCursor newCursor)
    {
        if (cursor != newCursor)
        {
            bool selectionChanged = newCursor.StackId != cursor.StackId;

            cursor = newCursor;
            ActionPromptText = newCursor.Action?.SelectionText;
            RefreshRenderRequest();
            UpdateCursorType();

            if (selectionChanged)
            {
                NotifySelectionCommandsChanged();
                UpdateStackActions();
            }
        }
    }

    private void SelectStack(WorldStackId? stackId)
    {
        SetCursor(cursor.SelectStack(stackId));
        UpdateReachable();
    }

    private void PlayStackResponseSound(WorldStack stack)
    {
        (WorldUnit leader, GridPosition _, UnitDefinition _) = stack.GetLeader(GameModel.Definitions);
        int variant = random.Next(AssetSoundKey.StackUnitResponse.VariantCount);

        Game.PlaySound(new AssetSoundKey.StackUnitResponse(leader.DefinitionId, variant));
    }

    private void FocusCamera(WorldPosition position, TimeSpan duration)
    {
        CameraFocus = position;
        CameraFocusRequested?.Invoke(position, duration);
    }

    private async Task MoveSelectedStackAsync(WorldStack stack, WorldPathSelection path)
    {
        if (path.Encounter is null)
        {
            await TryDispatchWorldActionAsync(new WorldMoveStackCommand(stack.Id, path.Destination));
        }
        else
        {
            await path.Encounter.Accept(this, new SelectedMove(stack, path));
        }
    }

    async Task IWorldPathEncounterVisitor<SelectedMove, Task>.Visit(WorldPathEncounter.Battle encounter, SelectedMove move)
    {
        WorldMoveStackCommand command = new(move.Stack.Id, move.Selection.Destination);

        if (encounter.TargetPosition == move.Selection.Destination)
        {
            await TryDispatchWorldActionAsync(command);
        }
        else
        {
            Game.OpenScreen(CreateAttackPrompt(encounter.TargetPosition, async () => await TryDispatchWorldActionAsync(command)));
        }
    }

    private AttackPromptViewModel CreateAttackPrompt(WorldPosition position, Func<Task> attackAction)
    {
        WorldPlan plan = WorldPlan.For(WorldState);
        WorldVisibility visibility = WorldVisibility.Create(WorldState, ViewPlayerId, FogEnabled);
        WorldStack? stack = visibility.GetStackAt(plan, position, ViewPlayerId);
        WorldFortification? fortification = visibility.GetFortificationAt(plan, position);

        if (stack is not null)
        {
            (WorldUnit leader, GridPosition _, UnitDefinition _) = stack.GetLeader(GameModel.Definitions);
            string name = UnitNameResolver.Resolve(leader, GameModel.Definitions, GameModel.Texts);
            AssetSpriteKey.UnitEventPortrait portraitKey = new(ScenarioEventPortraitId.Unit(leader.DefinitionId));

            return new AttackPromptViewModel(portraitKey, name, true, attackAction);
        }
        else if (fortification is not null)
        {
            string name = GameModel.Texts.GetScenarioText(fortification.NameTextId);

            return new AttackPromptViewModel(null, name, true, attackAction);
        }
        else
        {
            return new AttackPromptViewModel(null, Tr("an unknown enemy"), false, attackAction);
        }
    }

    async Task IWorldPathEncounterVisitor<SelectedMove, Task>.Visit(WorldPathEncounter.Thief encounter, SelectedMove move)
    {
        ImmutableArray<WorldPosition> path = move.Selection.Path.Positions;

        if (!path.IsEmpty && path[^1] == encounter.TargetPosition)
        {
            path = path[..^1];
        }

        if (path.IsEmpty || await TryDispatchWorldActionAsync(new WorldMoveStackCommand(move.Stack.Id, path[^1])))
        {
            EnqueueEntry(new Entry.ThiefAction(WorldState, move.Stack.Id, encounter.TargetPosition));
        }
    }

    async Task IWorldPathEncounterVisitor<SelectedMove, Task>.Visit(WorldPathEncounter.Bag encounter, SelectedMove move)
    {
        await TryDispatchWorldActionAsync(new WorldPickUpBagCommand(move.Stack.Id, encounter.BagId));
    }

    async Task IWorldPathEncounterVisitor<SelectedMove, Task>.Visit(WorldPathEncounter.Site encounter, SelectedMove move)
    {
        if (await TryDispatchWorldActionAsync(new WorldMoveStackCommand(move.Stack.Id, move.Selection.Destination)))
        {
            EnqueueEntry(new Entry.SiteEnter(WorldState, move.Stack.Id, encounter.SiteId));
        }
    }

    public void Update(TimeSpan delta, TimeSpan animationElapsed)
    {
        if (IsActive)
        {
            WorldAnimationState.FocusInfo? focusInfo = RenderRequest.Animation?.GetFocusInfo(animationElapsed);

            if (focusInfo is { } value && value.Position != CameraFocus)
            {
                FocusCamera(value.Position, value.Duration);
            }

            UpdateStackIdleTimer(delta);
        }
    }

    private void UpdateStackIdleTimer(TimeSpan delta)
    {
        stackIdleRemaining -= delta;

        if (stackIdleRemaining <= TimeSpan.Zero)
        {
            WorldStack? stack = ResolveSelectedStack();

            if (stack is not null)
            {
                EnqueueEntry(new Entry.StackIdle(WorldState, stack.Id));
            }

            RestartStackIdleTimer();
        }
    }

    private void RestartStackIdleTimer()
    {
        stackIdleRemaining = StackIdleDelay + (StackIdleInterval * random.NextDouble());
    }

    public void CompleteAnimation()
    {
        if (eventQueue.PlayingEntry is Entry.Animated ||
            eventActionPlayer.ActiveAnimation is WorldAnimationState.BattleEnd)
        {
            StopEntry();
            ProcessEventQueue();
        }
    }

    public void CompleteGameStart()
    {
        if (eventQueue.PlayingEntry is Entry.GameStart)
        {
            AbstractScreenViewModel? activeScreen = eventActionPlayer.ActiveScreen;

            if (activeScreen is not null)
            {
                Game.CloseScreen(activeScreen);
            }
        }
    }

    private void EnqueueEntry(Entry entry)
    {
        eventQueue.EnqueueEntry(entry);
        ProcessEventQueue();
    }

    private void ProcessEventQueue()
    {
        if (IsActive)
        {
            WorldEventActionBuilder builder = new(
                GameModel.Definitions,
                GameModel.Texts,
                GameModel.Scenario,
                random,
                Snapshot,
                GameModel.ActiveBattle,
                Game.Settings,
                FogEnabled);

            while (eventQueue.PlayNext() is { } entry)
            {
                WorldEventAction action = builder.Build(entry);

                foreach (AssetSoundKey sound in action.Sounds)
                {
                    Game.PlaySound(sound);
                }

                action.Accept(eventActionPlayer);

                if (action is WorldEventAction.Blocking blocking)
                {
                    if (blocking.FocusPosition is not null)
                    {
                        FocusCamera(blocking.FocusPosition.Value, TimeSpan.Zero);
                    }

                    break;
                }

                StopEntry();
            }
        }

        RefreshRenderRequest();
        UpdateCursorType();
        UpdateEndTurnHighlighted();
        UpdateActionWheelVisible();

        if (eventQueue.IsIdle)
        {
            if (IsActive && Snapshot.IsGameEnded)
            {
                ShowGameEndedScreen();
            }

            NotifySnapshotGatedCommandsChanged();
            NotifySelectionCommandsChanged();
        }
    }

    private void OnActionAutoSave(WorldEventAction.AutoSave action)
    {
        Game.TryAutoSaveAsync(GameModel, action.Name, action.Type, WorldState).FileAndForget();
    }

    private void OnActionTurnStart(WorldEventAction.TurnStart action)
    {
        if (action.Save is not null)
        {
            OnActionAutoSave(action.Save);
        }

        WorldStack? selected = ResolveSelectedStack();
        WorldPosition? focusPosition;

        if (selected is not null)
        {
            focusPosition = selected.Position;
        }
        else if (StackItems.Count > 0)
        {
            focusPosition = StackItems[0].Position;
        }
        else
        {
            WorldPlan plan = WorldPlan.For(WorldState);
            WorldCapital? capital = plan.GetCapital(ViewPlayerId);

            focusPosition = capital?.Entrance;
        }

        if (focusPosition is not null)
        {
            FocusCamera(focusPosition.Value, TimeSpan.Zero);
        }
    }

    private void OnActionObjectiveChange(WorldEventAction.ObjectiveChange action)
    {
        ObjectiveText = action.Text;
        IsObjectiveExpanded = true;
        ObjectiveChanged?.Invoke();
    }

    private void OnActionFortificationExit(WorldEventAction.FortificationExit action)
    {
        SetCursor(cursor.SelectStack(action.StackId));
        UpdateReachable();
    }

    private void OnActionStackExhaust(WorldEventAction.StackExhaust action)
    {
        if (cursor.StackId == action.StackId)
        {
            WorldStackItemViewModel? nextStack = FindNextUnfinishedStackItem(action.StackId);

            if (nextStack is not null)
            {
                ClearSelectedAction();
                FocusStack(WorldState.Stacks[nextStack.StackId]);
            }
        }
    }

    private void ShowGameEndedScreen()
    {
        WorldPlayerId? winnerId = WorldState.ScenarioState.Winner;
        ScenarioInfoTexts infoTexts = GameModel.Scenario.Definition.InfoTexts;
        WorldRace race = WorldState.Players[ViewPlayerId].Race;
        bool isVictory = winnerId is not null && winnerId.Value == ViewPlayerId;

        ScenarioTextId debriefingTextId;
        AssetSoundKey music;

        if (isVictory)
        {
            debriefingTextId = infoTexts.WinDebriefing;
            music = new AssetSoundKey.VictoryMusic(new ScenarioId(GameModel.ScenarioId));
        }
        else
        {
            debriefingTextId = infoTexts.LoseDebriefing;
            music = new AssetSoundKey.BriefingMusic();
        }

        Game.OpenScreen(new GameEndedScreenViewModel(
            isVictory,
            GameModel.Texts.GetScenarioText(infoTexts.Name),
            GameModel.Texts.GetScenarioText(debriefingTextId),
            new ScenarioTextFormatter(GameModel.Scenario.Names),
            MenuBackgroundViewModel.TryCreateRaceSelection(race),
            ScreenMusic.Tracks(MusicQueue.Create([music], isLooping: false)),
            Game,
            canContinueCampaign: isVictory && GameModel.Scenario.Definition.NextScenarioId is not null));
    }

    private bool CanDispatchCommand()
    {
        return !IsBusy;
    }

    private void NotifySelectionCommandsChanged()
    {
        InspectStackCommand.NotifyCanExecuteChanged();
        FinishStackCommand.NotifyCanExecuteChanged();
        OpenStackCommand.NotifyCanExecuteChanged();
    }

    protected override void OnSnapshotChanged(GameSnapshot current)
    {
        UpdateWorld();

        ProcessEventQueue();
    }

    private void UpdateWorld()
    {
        UpdateSelection();
        UpdateReachable();
        UpdatePositionInfo();
        UpdateStatus();
        UpdateCursorType();
        UpdateStackActions();
    }

    private void UpdateStackActions()
    {
        WorldStack? selected = ResolveSelectedStack();
        WorldVisibility visibility = WorldVisibility.Create(WorldState, ViewPlayerId, FogEnabled);
        bool viewPlayerTurn = Snapshot.IsViewPlayerTurn;
        List<WorldStack> stacks = [];

        foreach (WorldStack stack in WorldState.Stacks.Values)
        {
            if (stack.OwnerId == ViewPlayerId && visibility.IsRevealed(stack.Position))
            {
                stacks.Add(stack);
            }
        }

        if (selected is not null)
        {
            ActionWheel = WorldActionWheelViewModel.TryCreate(
                WorldState,
                selected,
                GameModel.Definitions,
                GameModel.Texts,
                GameModel.Stats);
        }
        else
        {
            ActionWheel = null;
        }

        IsViewPlayerTurn = viewPlayerTurn;

        UpdateActionWheelVisible();
        UpdateStackItems(stacks);
    }

    private void UpdateActionWheelVisible()
    {
        IsActionWheelVisible = ActionWheel is not null && IsActive && !IsBusy;
    }

    private void UpdateEndTurnHighlighted()
    {
        IsEndTurnHighlighted = Snapshot.IsViewPlayerTurn && eventQueue.IsIdle && !HasUnfinishedStack();
    }

    private bool HasUnfinishedStack()
    {
        foreach (WorldStack stack in WorldState.Stacks.Values)
        {
            if (stack.OwnerId == ViewPlayerId && stack.MovementPoints > 0 && !IsFinished(stack))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateStackItems(List<WorldStack> stacks)
    {
        List<WorldStackItemViewModel> newItems = [];

        foreach (WorldStack stack in stacks)
        {
            WorldStackItemViewModel newItem = new(stack.Id);
            newItem.Update(stack, stack.Id == cursor.StackId, IsFinished(stack), GameModel.Definitions, GameModel.Texts, GameModel.Stats);
            newItems.Add(newItem);
        }

        int index = 0;

        foreach (CollectionJoin.Item<WorldStackItemViewModel> item in CollectionJoin.Make(StackItems, newItems, item => (-item.Level, item.StackId)))
        {
            if (item.Right is null)
            {
                StackItems.RemoveAt(index);
            }
            else if (item.Left is null)
            {
                StackItems.Insert(index, item.Right);
                index++;
            }
            else
            {
                WorldStack stack = WorldState.Stacks[item.Left.StackId];
                item.Left.Update(stack, stack.Id == cursor.StackId, IsFinished(stack), GameModel.Definitions, GameModel.Texts, GameModel.Stats);
                index++;
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanDispatchCommand))]
    private async Task ActivateStackActionAsync(WorldStackAction action)
    {
        ClearSelectedAction();

        await action.Accept(this);
    }

    async Task IWorldStackActionVisitor<Task>.Visit(WorldStackAction.Exchange action)
    {
        Game.OpenScreen(ManagementScreenViewModel.CreateForExchange(GameModel, Game, action.StackId));
    }

    async Task IWorldStackActionVisitor<Task>.Visit(WorldStackAction.LearnAbility action)
    {
        Game.OpenScreen(new LeaderUpgradeViewModel(GameModel, Game, action.StackId));
    }

    async Task IWorldStackActionVisitor<Task>.Visit(WorldStackAction.CreateRod action)
    {
        SelectAction(new WorldCursorAction.CreateRod(action.StackId));
    }

    async Task IWorldStackActionVisitor<Task>.Visit(WorldStackAction.DestroyRod action)
    {
        SelectAction(new WorldCursorAction.DestroyRod(action.StackId));
    }

    async Task IWorldStackActionVisitor<Task>.Visit(WorldStackAction.UseItem action)
    {
        if (!WorldState.Stacks.TryGetValue(action.StackId, out WorldStack? stack))
        {
            return;
        }

        LeaderEquipmentSlot? slot = stack.GetEquipmentSlot(action.ItemId);

        if (slot is null)
        {
            return;
        }

        WorldItem item = stack.Equipment.Items[slot.Value];
        ItemDefinition definition = GameModel.Definitions.GetItem(item.DefinitionId);

        if (definition.Effect is SpellItemEffect effect)
        {
            WorldSpellDefinition spell = GameModel.Definitions.GetSpell(effect.SpellId);

            if (spell.AreaRadius is null)
            {
                if (await TryDispatchWorldActionAsync(new WorldUseItemCommand(action.StackId, action.ItemId, Target: null)))
                {
                    Game.PlaySound(ItemSoundResolver.ResolveWorld(definition.Category));
                }
            }
            else
            {
                SelectAction(new WorldCursorAction.UseItem(action.StackId, action.ItemId));
            }
        }
    }

    private void UpdateCursorType()
    {
        if (IsBusy)
        {
            WorldState state = ResolveDisplayedState();

            if (state.TurnInfo.CurrentPlayerId == ViewPlayerId)
            {
                CursorType = AssetAnimationKey.CursorType.Default;
            }
            else
            {
                CursorType = AssetAnimationKey.CursorType.Wait;
            }
        }
        else if (cursor.HoverTile is null)
        {
            CursorType = AssetAnimationKey.CursorType.Default;
        }
        else if (cursor.Action is not null)
        {
            CursorType = cursor.Action.Accept(cursorTypeVisitor, cursor.HoverTile.Value);
        }
        else
        {
            CursorType = ResolveHoverCursor(cursor.HoverTile.Value);
        }
    }

    private AssetAnimationKey.CursorType ResolveHoverCursor(WorldPosition tile)
    {
        WorldPlan plan = WorldPlan.For(WorldState);
        WorldVisibility visibility = WorldVisibility.Create(WorldState, ViewPlayerId, FogEnabled);
        WorldStack? selectedStack = ResolveSelectedStack();
        WorldPosition position = ResolveClickTarget(tile);
        WorldStack? stack = visibility.GetStackAt(plan, position, ViewPlayerId);
        WorldFortification? fortification = visibility.GetFortificationAt(plan, position);
        WorldBag? bag = visibility.GetBagAt(plan, position);
        WorldSite? site = visibility.GetSiteAt(plan, position);

        if (stack is not null && stack.OwnerId == ViewPlayerId)
        {
            if (cursor.StackId != stack.Id)
            {
                return AssetAnimationKey.CursorType.SelectUnit;
            }
            else if (fortification is not null && fortification.Owner == ViewPlayerId)
            {
                return AssetAnimationKey.CursorType.ViewCity;
            }
            else
            {
                return AssetAnimationKey.CursorType.Default;
            }
        }
        else if (selectedStack is not null &&
                 !cursor.ReachablePositions.Contains(position) &&
                 (fortification is null || fortification.Owner != ViewPlayerId))
        {
            return AssetAnimationKey.CursorType.Default;
        }
        else if (fortification is not null)
        {
            if (fortification.Owner != ViewPlayerId)
            {
                if (selectedStack is not null)
                {
                    if (IsThief(selectedStack))
                    {
                        return AssetAnimationKey.CursorType.Negotiate;
                    }
                    else if (fortification is WorldRuin { LooterId: not null })
                    {
                        return AssetAnimationKey.CursorType.Move;
                    }
                    else
                    {
                        return AssetAnimationKey.CursorType.CastSpell;
                    }
                }
                else
                {
                    return AssetAnimationKey.CursorType.Default;
                }
            }
            else if (position == fortification.Entrance && selectedStack is not null)
            {
                return AssetAnimationKey.CursorType.EnterCity;
            }
            else
            {
                return AssetAnimationKey.CursorType.ViewCity;
            }
        }
        else if (selectedStack is null)
        {
            return AssetAnimationKey.CursorType.Default;
        }
        else if (stack is not null)
        {
            if (IsThief(selectedStack))
            {
                return AssetAnimationKey.CursorType.Negotiate;
            }
            else
            {
                return AssetAnimationKey.CursorType.CastSpell;
            }
        }
        else if (bag is not null)
        {
            return AssetAnimationKey.CursorType.TakeBag;
        }
        else if (site is WorldMerchantSite or WorldMageSite && IsThief(selectedStack))
        {
            return AssetAnimationKey.CursorType.Negotiate;
        }
        else if (site is not null)
        {
            return AssetAnimationKey.CursorType.Visit;
        }
        else if (visibility.IsRevealed(position) && plan.IsObstacleAt(position))
        {
            return AssetAnimationKey.CursorType.Default;
        }
        else
        {
            return AssetAnimationKey.CursorType.Move;
        }

        bool IsThief(WorldStack targetStack)
        {
            (WorldUnit _, GridPosition _, UnitDefinition leaderDefinition) = targetStack.GetLeader(GameModel.Definitions);

            return leaderDefinition.LeaderCategory == UnitLeaderCategory.Thief;
        }
    }

    private WorldPosition? ResolveHoverTarget(WorldPosition tile)
    {
        WorldPosition position = ResolveClickTarget(tile);

        if (ResolveFortificationBodyAt(position) is null)
        {
            return position;
        }
        else
        {
            return null;
        }
    }

    private WorldPosition ResolvePointerTile(WorldPosition tile, IReadOnlyList<WorldPosition> objectPositions)
    {
        if (cursor.Action is null && objectPositions.Count == 1)
        {
            WorldPlan plan = WorldPlan.For(WorldState);
            WorldStack? stack = plan.GetStackAt(objectPositions[0]);

            if (stack is not null && stack.OwnerId == ViewPlayerId && stack.Id != cursor.StackId)
            {
                return stack.Position;
            }
        }

        return tile;
    }

    private WorldPosition ResolveClickTarget(WorldPosition tile)
    {
        WorldPlan plan = WorldPlan.For(WorldState);
        WorldVisibility visibility = WorldVisibility.Create(WorldState, ViewPlayerId, FogEnabled);
        WorldFortification? fortification = visibility.GetFortificationAt(plan, tile);
        WorldSite? site = visibility.GetSiteAt(plan, tile);

        if (fortification is not null && fortification.Owner != ViewPlayerId)
        {
            return fortification.Entrance;
        }
        else if (site is not null)
        {
            return site.Entrance;
        }
        else
        {
            return tile;
        }
    }

    private void UpdateStatus()
    {
        Status = WorldStatusViewModel.Create(WorldState, ViewPlayerId, Status);
    }

    private void UpdateObjective()
    {
        ScenarioTextId? textId = WorldState.ScenarioState.ObjectiveTextId;

        if (textId is not null)
        {
            string text = GameModel.Texts.GetScenarioText(textId.Value);

            ObjectiveText = ScenarioFormatter.FormatObjective(text);
        }
        else
        {
            ObjectiveText = null;
        }
    }

    private void UpdateSelection()
    {
        cursor = cursor.ReconcileWith(WorldState);

        WorldStack? selected = ResolveSelectedStack();

        if (selected is not null && selected.OwnerId != ViewPlayerId)
        {
            cursor = cursor.SelectStack(null);
        }
    }

    private void UpdateReachable()
    {
        WorldStack? selected = ResolveSelectedStack();
        ImmutableArray<WorldPosition> reachable = [];

        if (selected is not null && Snapshot.IsViewPlayerTurn)
        {
            ICommandResult<WorldCommandError, WorldEvent, WorldGetMoveInfoCommand.Result> result =
                GameModel.World.DryRunCommand(new WorldGetMoveInfoCommand(selected.Id));

            if (!result.Failed)
            {
                WorldGetMoveInfoCommand.Result data = result.Data;

                reachable = data.ReachablePositions;
            }
        }

        WorldCursor updated = cursor with { ReachablePositions = reachable, HoverPath = null };

        SetCursor(updated with { HoverPath = ResolveHoverPath(updated, updated.HoverTarget) });
    }

    private static MusicQueue BuildWorldMusicQueue(IRandomProvider random)
    {
        List<AssetSoundKey> worldTracks = [];
        for (int variant = 0; variant < AssetSoundKey.WorldMusic.VariantCount; variant++)
        {
            worldTracks.Add(new AssetSoundKey.WorldMusic(variant));
        }

        List<AssetSoundKey> ambientTracks = [];
        for (int variant = 0; variant < AssetSoundKey.AmbientMusic.VariantCount; variant++)
        {
            ambientTracks.Add(new AssetSoundKey.AmbientMusic(variant));
        }

        return new MusicQueue.Builder(random)
            .AddShuffled(worldTracks)
            .AddShuffled(ambientTracks)
            .Build();
    }

    private WorldPosition ResolveInitialFocusPosition()
    {
        WorldPlan plan = WorldPlan.For(WorldState);

        if (plan.GetCapital(ViewPlayerId) is { } capital)
        {
            return capital.Entrance;
        }
        else
        {
            WorldVisibility visibility = WorldVisibility.Create(WorldState, ViewPlayerId, FogEnabled);

            foreach (WorldStack stack in WorldState.Stacks.Values)
            {
                if (stack.OwnerId == ViewPlayerId && visibility.IsRevealed(stack.Position))
                {
                    return stack.Position;
                }
            }

            return new WorldPosition(WorldState.Map.Width / 2, WorldState.Map.Height / 2);
        }
    }

    private void StopEntry()
    {
        eventQueue.Stop();
        eventActionPlayer.Stop();
    }

    private WorldState ResolveDisplayedState()
    {
        Entry? entry = eventQueue.PlayingEntry;

        if (entry is not null)
        {
            return entry.State;
        }
        else
        {
            return WorldState;
        }
    }

    private void RefreshRenderRequest()
    {
        WorldState state = ResolveDisplayedState();

        SetRenderRequest(new WorldRenderRequest(
            state,
            ViewPlayerId,
            cursor,
            FogEnabled,
            eventActionPlayer.ActiveAnimation));
    }

    private void SetRenderRequest(WorldRenderRequest request)
    {
        if (SetProperty(ref renderRequest, request, nameof(RenderRequest)))
        {
            IsInBattle = request.Animation is WorldAnimationState.BattleStart;
            FastForwardCommand.NotifyCanExecuteChanged();

            if (request.State.TurnInfo.CurrentPlayerId == ViewPlayerId)
            {
                IsFastForward = false;
            }
        }
    }

    private WorldPathSelection? BuildPathSelection(WorldStack stack, WorldPosition target)
    {
        ICommandResult<WorldCommandError, WorldEvent, WorldGetPathCommand.Result> result =
            GameModel.World.DryRunCommand(new WorldGetPathCommand(stack.Id, target));

        if (result.Failed || result.Data is null)
        {
            return null;
        }

        WorldGetPathCommand.Result data = result.Data;

        return new WorldPathSelection(target, data.Path, data.Remainder, data.Encounter);
    }

    private WorldStack? ResolveSelectedStack()
    {
        if (cursor.StackId is not null)
        {
            return WorldState.Stacks.GetValueOrDefault(cursor.StackId.Value);
        }
        else
        {
            return null;
        }
    }

    private WorldFortification? ResolveFortificationBodyAt(WorldPosition position)
    {
        WorldPlan plan = WorldPlan.For(WorldState);
        WorldVisibility visibility = WorldVisibility.Create(WorldState, ViewPlayerId, FogEnabled);
        WorldFortification? fortification = visibility.GetFortificationAt(plan, position);

        if (fortification is not null && position != fortification.Entrance)
        {
            return fortification;
        }
        else
        {
            return null;
        }
    }
}
