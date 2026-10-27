using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class BattleScreenViewModel : AbstractGameScreenViewModel
{
    private sealed record TileAction(IBattleCommand<Empty> Command, BattleActionTarget Target);

    private readonly IAssetMetadataProvider assets;
    private readonly IAssetPreloader assetPreloader;
    private readonly IRandomProvider random;
    private readonly AssetSpriteKey.BattleBackground backgroundKey;
    private readonly BattleEnvironment environment;
    private readonly BattleModel battle;
    private readonly MusicQueue musicQueue;
    private readonly CleanupList assetLeases;
    private readonly AssetPreloadPlan assetPlan;
    private readonly string? fortificationName;
    private readonly BattleLogFormatter logFormatter;
    private LeaderEquipmentSlot? activeItemSlot;
    private TaskCompletionSource<TimeSpan>? animationCompletionSource;
    private BattleUnitId? inspectedUnitId;
    private LeaderEquipmentSlot? inspectedItemSlot;
    private bool isActing;
    private bool canWait;
    private bool canRetreat;
    private BattleRenderState? renderState;
    private BattleGridTile? hoveredTile;
    private BattleGridTile? selectedTile;
    private IReadOnlyDictionary<BattleGridTile, TileAction> attackActions =
        new Dictionary<BattleGridTile, TileAction>();
    private IReadOnlyDictionary<LeaderEquipmentSlot, IReadOnlyDictionary<BattleGridTile, TileAction>> itemActions =
        new Dictionary<LeaderEquipmentSlot, IReadOnlyDictionary<BattleGridTile, TileAction>>();
    private IReadOnlyDictionary<BattleGridTile, TileAction> tileActions =
        new Dictionary<BattleGridTile, TileAction>();

    public BattleScreenViewModel(
        GameModel gameModel,
        BattleModel battle,
        GameViewModel gameViewModel,
        IAssetMetadataProvider assets,
        IAssetPreloader assetPreloader,
        IRandomProvider random)
        : base(gameModel, gameViewModel)
    {
        this.battle = battle;
        this.assets = assets;
        this.assetPreloader = assetPreloader;
        this.random = random;

        logFormatter = new BattleLogFormatter(gameModel.Definitions, gameModel.Texts);

        AssetSpriteKey.BattleLocation location = ResolveBattleLocation(gameModel, battle.State, battle.Position);
        fortificationName = ResolveFortificationName(gameModel, battle.Position);

        AttackerGrid = new BattleGridViewModel();
        DefenderGrid = new BattleGridViewModel();

        List<AssetSoundKey> musicTracks = [];

        for (int variant = 0; variant < AssetSoundKey.BattleMusic.VariantCount; variant++)
        {
            musicTracks.Add(new AssetSoundKey.BattleMusic(variant));
        }

        musicQueue = new MusicQueue.Builder(random).AddShuffled(musicTracks).Build();
        backgroundKey = new AssetSpriteKey.BattleBackground(location, SelectBackgroundVariant(battle.Position));
        environment = SelectEnvironment(random);
        assetLeases = new CleanupList();

        ResetTarget();
        UpdateState();

        BattleAssetPreloadPlanBuilder planBuilder = new(RenderState, battle.State, gameModel.Definitions, assets);
        assetPlan = planBuilder.Build();

        RegisterSnapshotGatedCommands(
            DefendCommand,
            WaitCommand,
            RetreatCommand,
            SkipCommand,
            AutoResolveCommand,
            ExitBattleCommand);
    }

    [ObservableProperty]
    public partial BattleGridViewModel AttackerGrid { get; private set; }

    [ObservableProperty]
    public partial BattleGridViewModel DefenderGrid { get; private set; }

    public ObservableCollection<BattleItemViewModel> Items { get; } = [];

    [ObservableProperty]
    public partial string StatusText { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string? ItemPromptText { get; private set; }

    public string AutoResolveTip => battle.CanAutoResolve ? Tr("Resolve the battle automatically") : Tr("This battle cannot be resolved automatically");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AttackerUnit), nameof(DefenderUnit))]
    public partial BattleUnitCardViewModel? CurrentUnit { get; private set; }

    public BattleUnitCardViewModel? AttackerUnit => SelectSideUnit(BattleSide.Attacker);

    public BattleUnitCardViewModel? DefenderUnit => SelectSideUnit(BattleSide.Defender);

    [ObservableProperty]
    public partial BattleUnitCardViewModel? TargetUnit { get; private set; }

    [ObservableProperty]
    public partial bool IsBattleOver { get; private set; }

    [ObservableProperty]
    public partial string OutcomeTitle { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial TextSpans OutcomeText { get; private set; } = TextSpans.Empty;

    [ObservableProperty]
    public partial IconMoniker OutcomeIcon { get; private set; }

    [ObservableProperty]
    public partial bool IsPlayerTurn { get; private set; }

    [ObservableProperty]
    public partial bool HasTargets { get; private set; }

    [ObservableProperty]
    public partial UnitDetailsViewModel? InspectedUnit { get; private set; }

    [ObservableProperty]
    public partial ItemDetailsViewModel? InspectedItem { get; private set; }

    [ObservableProperty]
    public partial BattleAnimationState? AnimationState { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PaintsOwnBackground))]
    public partial BattleWorldTransitionState? TransitionState { get; private set; } = new(BattleTransitionType.In);

    public override bool PaintsOwnBackground => TransitionState is null;

    public override ScreenMusic Music => ScreenMusic.Tracks(musicQueue);

    public BattleRenderState RenderState
    {
        get
        {
            Debug.Assert(renderState is not null);
            return renderState;
        }
        private set => SetProperty(ref renderState, value);
    }

    private BattleState State
    {
        get
        {
            BattleState? state = Snapshot.BattleState;
            Debug.Assert(state is not null);
            return state;
        }
    }

    protected override bool IsBusy => base.IsBusy || isActing || TransitionState is not null;

    private bool IsFinished => State.Outcome is not null;

    public async Task LoadAsync()
    {
        await PreloadAssetsAsync([.. assetPlan.InitialRequired, .. assetPlan.InitialOptional]);
    }

    protected override void OnDispose()
    {
        GameModel.DismissBattle();
        assetLeases.Dispose();
    }

    protected override bool TryClose()
    {
        if (activeItemSlot is not null)
        {
            CancelItem();

            return true;
        }
        else if (CanExitBattle())
        {
            ExitBattle();

            return true;
        }
        else
        {
            Game.OpenScreen(new GameMenuViewModel(GameModel, Game));

            return true;
        }
    }

    public async Task ClickAsync(BattleGridTile tile)
    {
        if (IsBusy || IsFinished || battle.IsAiTurn)
        {
            return;
        }

        if (tileActions.TryGetValue(tile, out TileAction? action))
        {
            await TakeTurnAsync(action.Command, animate: true);
        }
    }

    public void SelectItem(LeaderEquipmentSlot itemSlot)
    {
        if (!CanTakeTurn())
        {
            return;
        }

        if (activeItemSlot == itemSlot)
        {
            SetActiveItemSlot(null);
        }
        else
        {
            SetActiveItemSlot(itemSlot);
        }
    }

    [RelayCommand]
    private void CancelItem()
    {
        SetActiveItemSlot(null);
    }

    private void SetActiveItemSlot(LeaderEquipmentSlot? itemSlot)
    {
        activeItemSlot = itemSlot;
        tileActions = ComputeTileActions();
        selectedTile = ResolveSelectedTile();
        RefreshView();
    }

    public void Hover(BattleGridTile? tile)
    {
        hoveredTile = tile;

        UpdateCursorType();

        if (IsBusy)
        {
            return;
        }

        BattleGridTile? target = ResolveSelectedTile();

        if (target != selectedTile)
        {
            selectedTile = target;
            RefreshView();
        }
    }

    public bool TryShowUnitDetails(BattleGridTile tile)
    {
        BattleUnit? unit = FindUnit(tile);

        if (unit is not null)
        {
            ShowUnitDetails(unit.Id);

            return true;
        }
        else
        {
            return false;
        }
    }

    public void ShowUnitDetails(BattleUnitId unitId)
    {
        inspectedUnitId = unitId;
        UpdateInspectedUnit();
    }

    public void HideUnitDetails()
    {
        inspectedUnitId = null;
        UpdateInspectedUnit();
    }

    public void ShowItemDetails(LeaderEquipmentSlot itemSlot)
    {
        inspectedItemSlot = itemSlot;
        UpdateInspectedItem();
    }

    public void HideItemDetails()
    {
        inspectedItemSlot = null;
        UpdateInspectedItem();
    }

    public void HandleRenderException(Exception ex)
    {
        GameModel.Log.Error("Failed to render battle.", ex);

        Game.ShowScreen(new ErrorScreenViewModel(
            Tr("Failed to render battle.\n{0}", ex.Message),
            Tr("Exit"),
            () => Game.ExitCommand.Execute(null)));
    }

    public void CompleteAnimation(TimeSpan loopTime)
    {
        animationCompletionSource?.TrySetResult(loopTime);
    }

    public async Task CompleteTransitionAsync()
    {
        Debug.Assert(TransitionState is not null);

        if (TransitionState.Type == BattleTransitionType.In)
        {
            TransitionState = null;
            RefreshView();

            PreloadAssetsAsync(assetPlan.Remaining).FileAndForget();

            await PlayAiTurnAsync();
        }
        else
        {
            CloseSelf();
        }
    }

    private async Task PlayAiTurnAsync()
    {
        try
        {
            while (true)
            {
                if (IsBusy || IsFinished || !battle.IsAiTurn)
                {
                    break;
                }
                else
                {
                    await DispatchBattleActionAsync(new BattleAiActionCommand(), animate: true);
                }
            }
        }
        catch (Exception ex)
        {
            GameModel.Log.Error("Failed to play the enemy turn.", ex);

            Game.ShowScreen(new ErrorScreenViewModel(
                Tr("Failed to play the enemy turn.\n{0}", ex.Message),
                Tr("Back"),
                () => Game.ShowMainMenuCommand.Execute(null)));
        }
    }

    [RelayCommand(CanExecute = nameof(CanTakeTurn))]
    private async Task DefendAsync()
    {
        await TakeTurnAsync(new BattleDefendCommand(State.CurrentUnitId), animate: true);
    }

    [RelayCommand(CanExecute = nameof(CanWait))]
    private async Task WaitAsync()
    {
        await TakeTurnAsync(new BattleWaitCommand(State.CurrentUnitId), animate: true);
    }

    [RelayCommand(CanExecute = nameof(CanRetreat))]
    private async Task RetreatAsync()
    {
        await TakeTurnAsync(new BattleRetreatCommand(State.CurrentUnitId), animate: true);
    }

    [RelayCommand(CanExecute = nameof(CanTakeTurn))]
    private async Task SkipAsync()
    {
        await TakeTurnAsync(new BattleSkipCommand(State.CurrentUnitId), animate: true);
    }

    [RelayCommand]
    private void SelectFirstItem()
    {
        SelectItemSlot(LeaderEquipmentSlot.Battle1);
    }

    [RelayCommand]
    private void SelectSecondItem()
    {
        SelectItemSlot(LeaderEquipmentSlot.Battle2);
    }

    private void SelectItemSlot(LeaderEquipmentSlot itemSlot)
    {
        foreach (BattleItemViewModel item in Items)
        {
            if (item.Slot == itemSlot && item.SelectCommand.CanExecute(null))
            {
                item.SelectCommand.Execute(null);
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanAutoResolve))]
    private void AutoResolve()
    {
        Game.OpenScreen(new ConfirmPromptViewModel(
            Tr("Auto-resolve battle"),
            TrSpans("Would you like to resolve the rest of the battle automatically?"),
            Tr("Resolve"),
            ConfirmPromptType.Primary,
            AutoResolveConfirmedAsync));
    }

    private async Task AutoResolveConfirmedAsync()
    {
        if (IsFinished)
        {
            return;
        }

        await battle.DispatchCommandAsync(new BattleAutoResolveCommand());
    }

    [RelayCommand]
    private void OpenLog()
    {
        Game.OpenScreen(new BattleLogPopupViewModel(logFormatter.Format(battle.History.Steps)));
    }

    [RelayCommand(CanExecute = nameof(CanExitBattle))]
    private void ExitBattle()
    {
        TransitionState = new BattleWorldTransitionState(BattleTransitionType.Out);
    }

    private bool CanTakeTurn()
    {
        if (IsBusy)
        {
            return false;
        }

        return !IsFinished && !battle.IsAiTurn;
    }

    private bool CanWait()
    {
        if (!CanTakeTurn())
        {
            return false;
        }

        return canWait;
    }

    private bool CanRetreat()
    {
        if (!CanTakeTurn())
        {
            return false;
        }

        return canRetreat;
    }

    private bool CanAutoResolve()
    {
        if (IsFinished || TransitionState is not null)
        {
            return false;
        }

        return battle.CanAutoResolve && (battle.IsAttackerAi || battle.IsDefenderAi);
    }

    private bool CanExitBattle()
    {
        return IsFinished;
    }

    protected override void OnSnapshotChanged(GameSnapshot current)
    {
        if (isActing || current.BattleState is null)
        {
            return;
        }

        UpdateState();
    }

    private async Task TakeTurnAsync(IBattleCommand<Empty> command, bool animate)
    {
        await DispatchBattleActionAsync(command, animate);
        await PlayAiTurnAsync();
    }

    private async Task DispatchBattleActionAsync(IBattleCommand<Empty> command, bool animate)
    {
        if (IsBusy)
        {
            return;
        }

        BattleState baseState = State;

        isActing = true;
        try
        {
            BattleCommandResult commandResult = await battle.DispatchCommandAsync(command);
            ResetTarget();

            if (!commandResult.Failed && animate)
            {
                await PlayAnimationAsync(baseState, commandResult.Events);
            }
        }
        finally
        {
            isActing = false;
        }

        UpdateState();
    }

    private async Task PreloadAssetsAsync(IEnumerable<AssetPreloadItem> items)
    {
        assetLeases.Add(await assetPreloader.PreloadAsync(items));
    }

    private async Task PlayAnimationAsync(BattleState baseState, IReadOnlyList<BattleEvent> events)
    {
        BattleRenderStateBuilder renderBuilder = new(
            baseState,
            State,
            GameModel.Stats,
            GameModel.Definitions,
            assets,
            backgroundKey,
            environment,
            battle.IsAiTurn,
            ResolveSelectedTarget(),
            tileActions.ToDictionary(entry => entry.Key, entry => entry.Value.Target),
            renderState);

        BattleAnimationStateBuilder animationBuilder = new(
            assets,
            GameModel.Definitions,
            random,
            battle.ViewerSide,
            Game.Settings.BattleSpeedFactor);

        BattleRenderState render = renderBuilder.BuildState();
        BattleAnimationState? animation = animationBuilder.Build(render, events);

        if (animation is not null)
        {
            List<AssetPreloadItem> preloadItems = [];

            foreach (AssetAnimationKey key in animation.Animations)
            {
                preloadItems.Add(new AssetPreloadItem.Animation(key));
            }

            foreach (AnimationSound sound in animation.Sequence.Sounds)
            {
                preloadItems.Add(new AssetPreloadItem.Sound(sound.Key));
            }

            await PreloadAssetsAsync(preloadItems);

            TaskCompletionSource<TimeSpan> completionSource = new();
            animationCompletionSource = completionSource;

            RenderState = render;
            AnimationState = animation;

            ILookup<AnimationTarget, AnimationProperty> properties = animation.Sequence.Properties.ToLookup(property => property.Target);

            IReadOnlyList<BattleGridUnitViewModel> attackerUnits = renderBuilder.BuildUnits(BattleSide.Attacker, true);
            AttackerGrid.UpdateUnits(attackerUnits.Select(unit => SetupUnitAnimation(unit, animation, properties)));

            IReadOnlyList<BattleGridUnitViewModel> defenderUnits = renderBuilder.BuildUnits(BattleSide.Defender, false);
            DefenderGrid.UpdateUnits(defenderUnits.Select(unit => SetupUnitAnimation(unit, animation, properties)));

            CurrentUnit = SetupCardAnimation(CurrentUnit, properties);
            TargetUnit = SetupCardAnimation(TargetUnit, properties);

            UpdateItems(BuildItems());
            NotifyCanExecuteChanged();

            TimeSpan loopTime = await completionSource.Task;

            animationCompletionSource = null;
            RenderState = RenderState.WithFinishedAnimation(animation, loopTime);
            AnimationState = null;
        }
    }

    private static BattleGridUnitViewModel SetupUnitAnimation(
        BattleGridUnitViewModel viewModel,
        BattleAnimationState animationState,
        ILookup<AnimationTarget, AnimationProperty> properties)
    {
        AnimationRange hitpoints = BuildHitpointsAnimation(viewModel.UnitId, properties);
        IReadOnlyList<BattleFeedbackViewModel> feedback = BuildFeedback(viewModel, animationState);
        if (hitpoints != AnimationRange.None || feedback.Count > 0)
        {
            return new BattleGridUnitViewModel(
                viewModel.UnitId,
                viewModel.Tile,
                viewModel.FaceKey,
                viewModel.Facing,
                viewModel.Row,
                viewModel.Column,
                viewModel.ColumnSpan,
                viewModel.Hitpoints,
                viewModel.MaxHitpoints,
                viewModel.Presence,
                viewModel.IsPendingUpgrade,
                viewModel.IsCurrent,
                viewModel.Marker,
                feedback,
                viewModel.Statuses,
                hitpoints);
        }
        else
        {
            return viewModel;
        }
    }

    private static BattleUnitCardViewModel? SetupCardAnimation(BattleUnitCardViewModel? card, ILookup<AnimationTarget, AnimationProperty> properties)
    {
        if (card is not null)
        {
            return card with { HitpointsAnimation = BuildHitpointsAnimation(card.UnitId, properties) };
        }
        else
        {
            return null;
        }
    }

    private static AnimationRange BuildHitpointsAnimation(BattleUnitId unitId, ILookup<AnimationTarget, AnimationProperty> properties)
    {
        return new AnimationRange(
            [.. properties[new BattleAnimationTarget.UnitHitpoints(unitId)]],
            [.. properties[new BattleAnimationTarget.UnitMaxHitpoints(unitId)]]);
    }

    private static IReadOnlyList<BattleFeedbackViewModel> BuildFeedback(BattleGridUnitViewModel viewModel, BattleAnimationState animationState)
    {
        List<BattleFeedbackViewModel> feedback = [];

        foreach (BattleAnimationState.Unit unit in animationState.Units)
        {
            if (unit.Id == viewModel.UnitId)
            {
                foreach (BattleAnimationState.FeedbackEntry entry in unit.Feedback)
                {
                    feedback.Add(new BattleFeedbackViewModel(entry.Feedback.Type, entry.Feedback.Text, entry.Animation));
                }
            }
        }

        return feedback;
    }

    private void ResetTarget()
    {
        selectedTile = null;
        activeItemSlot = null;
    }

    private IReadOnlyDictionary<BattleGridTile, TileAction> ComputeTileActions()
    {
        if (activeItemSlot is { } itemSlot)
        {
            return itemActions[itemSlot];
        }
        else
        {
            return attackActions;
        }
    }

    private IReadOnlyDictionary<BattleGridTile, TileAction> ComputeAttackActions()
    {
        if (IsFinished || battle.IsAiTurn)
        {
            return new Dictionary<BattleGridTile, TileAction>();
        }

        ICommandResult<BattleCommandError, BattleEvent, BattleGetAttackInfoCommand.Result> infoResult =
            battle.DryRunCommand(new BattleGetAttackInfoCommand(State.CurrentUnitId));

        if (infoResult.Failed)
        {
            return new Dictionary<BattleGridTile, TileAction>();
        }

        Dictionary<BattleAttackTarget, TileAction> actions = [];

        foreach (BattleGetAttackInfoCommand.AttackInfo attackInfo in infoResult.Data.AttackInfos)
        {
            if (attackInfo.Error is null)
            {
                BattleAttackCommand attackCommand = new(State.CurrentUnitId, attackInfo.Target);
                BattleActionTarget actionTarget = ResolveActionTarget(attackInfo.Targets);

                actions[attackInfo.Target] = new TileAction(attackCommand, actionTarget);
            }
        }

        return ResolveTileActions(actions);
    }

    private IReadOnlyDictionary<BattleGridTile, TileAction> ResolveTileActions(IReadOnlyDictionary<BattleAttackTarget, TileAction> targetActions)
    {
        Dictionary<BattleGridTile, TileAction> actions = [];

        foreach (BattleGridTile tile in EnumerateAllTiles())
        {
            BattleUnit? unit = FindUnit(tile);

            BattleAttackTarget target;
            if (unit is not null)
            {
                target = new BattleAttackUnitTarget(unit.Id);
            }
            else
            {
                target = new BattleAttackTileTarget(tile.Side, tile.Position);
            }

            if (targetActions.TryGetValue(target, out TileAction? action))
            {
                actions[tile] = action;
            }
        }

        return actions;
    }

    private static BattleActionTarget ResolveActionTarget(IReadOnlyList<BattleAttackTarget> targets)
    {
        IEnumerable<BattleAttackTileTarget> tileTargets = targets.GetTileTargets();

        List<BattleGridTile> summonTiles = [];

        foreach (BattleAttackTileTarget tile in tileTargets)
        {
            summonTiles.Add(new BattleGridTile(tile.Side, tile.Position));
        }

        if (summonTiles.Count > 0)
        {
            return new BattleActionTarget.Summon(summonTiles);
        }
        else
        {
            return new BattleActionTarget.Unit([.. targets.GetUnitIds()]);
        }
    }

    private static IEnumerable<BattleGridTile> EnumerateAllTiles()
    {
        foreach (BattleSide side in Enum.GetValues<BattleSide>())
        {
            foreach (GridPosition position in GridPosition.All)
            {
                yield return new BattleGridTile(side, position);
            }
        }
    }

    private IReadOnlyDictionary<LeaderEquipmentSlot, IReadOnlyDictionary<BattleGridTile, TileAction>> ComputeItemActions()
    {
        Dictionary<LeaderEquipmentSlot, IReadOnlyDictionary<BattleGridTile, TileAction>> actions = [];

        if (IsFinished || battle.IsAiTurn)
        {
            return actions;
        }

        BattleUnitId unitId = State.CurrentUnitId;
        ICommandResult<BattleCommandError, BattleEvent, BattleGetItemInfoCommand.Result> infoResult =
            battle.DryRunCommand(new BattleGetItemInfoCommand(unitId));

        if (infoResult.Failed)
        {
            return actions;
        }

        foreach (BattleGetItemInfoCommand.SlotInfo slotInfo in infoResult.Data.Slots)
        {
            actions[slotInfo.Slot] = ComputeItemActions(unitId, slotInfo);
        }

        return actions;
    }

    private IReadOnlyDictionary<BattleGridTile, TileAction> ComputeItemActions(BattleUnitId unitId, BattleGetItemInfoCommand.SlotInfo slotInfo)
    {
        Dictionary<BattleAttackTarget, TileAction> targetActions = [];

        foreach (BattleGetItemInfoCommand.ItemInfo itemInfo in slotInfo.ItemInfos)
        {
            if (itemInfo.Error is null)
            {
                BattleUseItemCommand useItemCommand = new(unitId, slotInfo.Slot, itemInfo.Target);
                BattleActionTarget actionTarget = ResolveActionTarget(itemInfo.Targets);

                targetActions[itemInfo.Target] = new TileAction(useItemCommand, actionTarget);
            }
        }

        return ResolveTileActions(targetActions);
    }

    private BattleUnit? FindUnit(BattleGridTile tile)
    {
        UnitGrid<BattleUnit> grid = State.GetGrid(tile.Side);

        return grid[tile.Position];
    }

    private IReadOnlyList<BattleItemViewModel> BuildItems()
    {
        if (IsFinished || battle.IsAiTurn)
        {
            return [];
        }

        List<BattleItemViewModel> result = [];

        foreach (BattleItem item in State.Items)
        {
            if (item.Side == State.CurrentUnit.Side)
            {
                if (itemActions.TryGetValue(item.Slot, out IReadOnlyDictionary<BattleGridTile, TileAction>? actions))
                {
                    bool canUse = !IsBusy && !item.Consumed && actions.Count > 0;

                    result.Add(new BattleItemViewModel(
                        this,
                        item.Slot,
                        FormatItemHotkey(item.Slot),
                        ItemFormatter.FormatName(GameModel.Definitions, GameModel.Texts, item.ItemId, item.Charges),
                        ItemIconResolver.Resolve(GameModel.Definitions, item.ItemId),
                        activeItemSlot == item.Slot,
                        canUse));
                }
            }
        }

        return result;
    }

    private static string FormatItemHotkey(LeaderEquipmentSlot itemSlot)
    {
        return itemSlot switch
        {
            LeaderEquipmentSlot.Battle1 => "1",
            LeaderEquipmentSlot.Battle2 => "2",
            LeaderEquipmentSlot.Banner or
                LeaderEquipmentSlot.Tome or
                LeaderEquipmentSlot.Artifact1 or
                LeaderEquipmentSlot.Artifact2 or
                LeaderEquipmentSlot.Boots => string.Empty
        };
    }

    private void UpdateItems(IReadOnlyList<BattleItemViewModel> items)
    {
        int index = 0;

        foreach (CollectionJoin.Item<BattleItemViewModel> item in CollectionJoin.Make(Items, items, button => (int)button.Slot))
        {
            if (item.Right is null)
            {
                Items.RemoveAt(index);
            }
            else if (item.Left is null)
            {
                Items.Insert(index, item.Right);
                index++;
            }
            else
            {
                Items[index].Update(item.Right);
                index++;
            }
        }
    }

    private BattleGridTile? ResolveSelectedTile()
    {
        if (hoveredTile is not null && tileActions.ContainsKey(hoveredTile))
        {
            return hoveredTile;
        }
        else
        {
            return null;
        }
    }

    private BattleActionTarget? ResolveSelectedTarget()
    {
        if (selectedTile is { } tile && tileActions.TryGetValue(tile, out TileAction? action))
        {
            return action.Target;
        }
        else
        {
            return null;
        }
    }

    private void UpdateState()
    {
        activeItemSlot = null;
        attackActions = ComputeAttackActions();
        itemActions = ComputeItemActions();
        tileActions = ComputeTileActions();
        selectedTile = ResolveSelectedTile();
        canWait = ComputeCanWait();
        canRetreat = ComputeCanRetreat();
        HasTargets = attackActions.Count > 0 || itemActions.Values.Any(actions => actions.Count > 0);
        RefreshView();
    }

    private bool ComputeCanWait()
    {
        BattleWaitCommand command = new(State.CurrentUnitId);
        BattleCommandResult commandResult = battle.DryRunCommand(command);

        return !commandResult.Failed;
    }

    private bool ComputeCanRetreat()
    {
        BattleRetreatCommand command = new(State.CurrentUnitId);
        BattleCommandResult commandResult = battle.DryRunCommand(command);

        return !commandResult.Failed;
    }

    private void RefreshView()
    {
        BattleRenderStateBuilder builder = new(
            State,
            State,
            GameModel.Stats,
            GameModel.Definitions,
            assets,
            backgroundKey,
            environment,
            battle.IsAiTurn,
            ResolveSelectedTarget(),
            tileActions.ToDictionary(entry => entry.Key, entry => entry.Value.Target),
            renderState);

        RenderState = builder.BuildState();
        ItemPromptText = activeItemSlot is not null ? Tr("Select a target for the item.") : null;
        AttackerGrid.UpdateUnits(builder.BuildUnits(BattleSide.Attacker, isFacingRight: true));
        AttackerGrid.UpdateTiles(builder.BuildTiles(BattleSide.Attacker, isFacingRight: true));
        DefenderGrid.UpdateUnits(builder.BuildUnits(BattleSide.Defender, isFacingRight: false));
        DefenderGrid.UpdateTiles(builder.BuildTiles(BattleSide.Defender, isFacingRight: false));
        UpdateItems(BuildItems());
        UpdateInspectedUnit();
        UpdateInspectedItem();
        UpdateCursorType();

        if (IsFinished)
        {
            ShowOutcomePanel();
        }
        else
        {
            ShowUnitPanel();
        }

        NotifyCanExecuteChanged();
    }

    private void UpdateCursorType()
    {
        if (IsBusy || IsFinished || battle.IsAiTurn)
        {
            CursorType = AssetAnimationKey.CursorType.BattleDefault;
        }
        else if (hoveredTile is null)
        {
            CursorType = AssetAnimationKey.CursorType.BattleDefault;
        }
        else if (activeItemSlot is not null)
        {
            CursorType = ResolveItemCursor(hoveredTile);
        }
        else
        {
            CursorType = ResolveAttackCursor(hoveredTile);
        }
    }

    private AssetAnimationKey.CursorType ResolveItemCursor(BattleGridTile tile)
    {
        if (tileActions.ContainsKey(tile))
        {
            return AssetAnimationKey.CursorType.SelectUnit;
        }
        else
        {
            return AssetAnimationKey.CursorType.BattleDefault;
        }
    }

    private AssetAnimationKey.CursorType ResolveAttackCursor(BattleGridTile tile)
    {
        UnitDefinition definition = GameModel.Definitions.GetUnit(State.CurrentUnit.DefinitionId);

        (AssetAnimationKey.CursorType valid, AssetAnimationKey.CursorType invalid) = GetCursor(definition.Attack.Type);

        if (tileActions.ContainsKey(tile))
        {
            return valid;
        }
        else if (FindUnit(tile) is not null)
        {
            return invalid;
        }
        else
        {
            return AssetAnimationKey.CursorType.BattleDefault;
        }

        static (AssetAnimationKey.CursorType Valid, AssetAnimationKey.CursorType Invalid) GetCursor(UnitAttackType type)
        {
            return type switch
            {
                UnitAttackType.Damage => (AssetAnimationKey.CursorType.CastSpell, AssetAnimationKey.CursorType.CastSpellInvalid),
                UnitAttackType.DrainLife => (AssetAnimationKey.CursorType.CastSpell, AssetAnimationKey.CursorType.CastSpellInvalid),
                UnitAttackType.Paralyze => (AssetAnimationKey.CursorType.CastSpell, AssetAnimationKey.CursorType.CastSpellInvalid),
                UnitAttackType.Heal => (AssetAnimationKey.CursorType.BattleHeal, AssetAnimationKey.CursorType.BattleHealInvalid),
                UnitAttackType.Fear => (AssetAnimationKey.CursorType.CastSpell, AssetAnimationKey.CursorType.CastSpellInvalid),
                UnitAttackType.IncreaseDamage => (AssetAnimationKey.CursorType.BattleHeal, AssetAnimationKey.CursorType.BattleHealInvalid),
                UnitAttackType.Petrify => (AssetAnimationKey.CursorType.CastSpell, AssetAnimationKey.CursorType.CastSpellInvalid),
                UnitAttackType.ReduceDamage => (AssetAnimationKey.CursorType.CastSpell, AssetAnimationKey.CursorType.CastSpellInvalid),
                UnitAttackType.ReduceInitiative => (AssetAnimationKey.CursorType.CastSpell, AssetAnimationKey.CursorType.CastSpellInvalid),
                UnitAttackType.Poison => (AssetAnimationKey.CursorType.CastSpell, AssetAnimationKey.CursorType.CastSpellInvalid),
                UnitAttackType.Frostbite => (AssetAnimationKey.CursorType.CastSpell, AssetAnimationKey.CursorType.CastSpellInvalid),
                UnitAttackType.Revive => (AssetAnimationKey.CursorType.BattleHeal, AssetAnimationKey.CursorType.BattleHealInvalid),
                UnitAttackType.DrainLifeOverflow => (AssetAnimationKey.CursorType.CastSpell, AssetAnimationKey.CursorType.CastSpellInvalid),
                UnitAttackType.Cure => (AssetAnimationKey.CursorType.BattleHeal, AssetAnimationKey.CursorType.BattleHealInvalid),
                UnitAttackType.Summon => (AssetAnimationKey.CursorType.SelectUnit, AssetAnimationKey.CursorType.BattleDefault),
                UnitAttackType.ReduceLevel => (AssetAnimationKey.CursorType.CastSpell, AssetAnimationKey.CursorType.CastSpellInvalid),
                UnitAttackType.GiveAdditionalAttack => (AssetAnimationKey.CursorType.BattleHeal, AssetAnimationKey.CursorType.BattleHealInvalid),
                UnitAttackType.Doppelganger => (AssetAnimationKey.CursorType.BattleTransform, AssetAnimationKey.CursorType.BattleTransformInvalid),
                UnitAttackType.TransformSelf => (AssetAnimationKey.CursorType.BattleTransform, AssetAnimationKey.CursorType.BattleTransformInvalid),
                UnitAttackType.TransformEnemy => (AssetAnimationKey.CursorType.BattleTransform, AssetAnimationKey.CursorType.BattleTransformInvalid),
                UnitAttackType.Blister => (AssetAnimationKey.CursorType.CastSpell, AssetAnimationKey.CursorType.CastSpellInvalid),
                UnitAttackType.GiveProtection => (AssetAnimationKey.CursorType.BattleHeal, AssetAnimationKey.CursorType.BattleHealInvalid),
                UnitAttackType.ReduceArmor => (AssetAnimationKey.CursorType.CastSpell, AssetAnimationKey.CursorType.CastSpellInvalid)
            };
        }
    }

    private void UpdateInspectedItem()
    {
        BattleItem? item = FindInspectedItem();

        if (item is not null)
        {
            InspectedItem = ItemDetailsViewModel.Create(
                item.ItemId,
                item.Charges,
                WorldState.Players[ViewPlayerId].Resources,
                GameModel.Definitions,
                GameModel.Texts);
        }
        else
        {
            inspectedItemSlot = null;
            InspectedItem = null;
        }
    }

    private BattleItem? FindInspectedItem()
    {
        if (inspectedItemSlot is null)
        {
            return null;
        }

        foreach (BattleItem item in State.Items)
        {
            if (item.Side == State.CurrentUnit.Side && item.Slot == inspectedItemSlot)
            {
                return item;
            }
        }

        return null;
    }

    private void UpdateInspectedUnit()
    {
        if (inspectedUnitId is { } unitId && State.TryGetUnit(unitId) is not null)
        {
            InspectedUnit = UnitDetailsViewModel.CreateFromBattle(
                State,
                unitId,
                GetOwner(State.GetUnit(unitId)),
                GameModel.Definitions,
                GameModel.Texts,
                GameModel.Stats);
        }
        else
        {
            inspectedUnitId = null;
            InspectedUnit = null;
        }
    }

    private WorldPlayer? GetOwner(BattleUnit unit)
    {
        bool isViewer = battle.ViewerSide switch
        {
            BattleViewerSide.Attacker => unit.Side == BattleSide.Attacker,
            BattleViewerSide.Defender => unit.Side == BattleSide.Defender,
            BattleViewerSide.None => false
        };

        if (isViewer)
        {
            return WorldState.Players[ViewPlayerId];
        }
        else
        {
            return null;
        }
    }

    private void ShowOutcomePanel()
    {
        CurrentUnit = null;
        TargetUnit = null;
        IsBattleOver = true;
        IsPlayerTurn = false;
        StatusText = string.Empty;
        (OutcomeTitle, OutcomeText, OutcomeIcon) = GetOutcome();
    }

    private void ShowUnitPanel()
    {
        CurrentUnit = BuildUnitCard(State.CurrentUnit);
        TargetUnit = BuildTargetUnit();
        IsBattleOver = false;
        IsPlayerTurn = !battle.IsAiTurn;
        StatusText = Tr("Round {0}", State.RoundNumber);
    }

    private BattleUnitCardViewModel? BuildTargetUnit()
    {
        BattleUnit? unit = selectedTile is null ? null : FindUnit(selectedTile);

        if (unit is not null)
        {
            return BuildUnitCard(unit);
        }
        else
        {
            return null;
        }
    }

    private BattleUnitCardViewModel? SelectSideUnit(BattleSide side)
    {
        if (CurrentUnit is not null && CurrentUnit.Side == side)
        {
            return CurrentUnit;
        }
        else
        {
            return null;
        }
    }

    private BattleUnitCardViewModel BuildUnitCard(BattleUnit unit)
    {
        UnitStats stats = GameModel.Stats.GetBattleUnitStats(State, unit.Id);
        IReadOnlyList<UnitModifierViewModel> modifiers = UnitModifierViewModel.CreateAll(unit, GameModel.Definitions);

        return new BattleUnitCardViewModel(
            unit.Id,
            unit.Side,
            new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(unit.DefinitionId)),
            new AssetSpriteKey.UnitPortrait(unit.DefinitionId),
            FormatUnitName(unit.Id),
            unit.Level,
            unit.Hitpoints,
            stats.MaxHitpoints,
            HitpointsAnimation: AnimationRange.None,
            UnitStatusViewModel.CreateAll(BattleUnitState.StatusEffect.CreateAll(unit.StatusEffects), modifiers));
    }

    private void NotifyCanExecuteChanged()
    {
        DefendCommand.NotifyCanExecuteChanged();
        WaitCommand.NotifyCanExecuteChanged();
        RetreatCommand.NotifyCanExecuteChanged();
        SkipCommand.NotifyCanExecuteChanged();
        AutoResolveCommand.NotifyCanExecuteChanged();
        ExitBattleCommand.NotifyCanExecuteChanged();
    }

    private (string Title, TextSpans Text, IconMoniker Icon) GetOutcome()
    {
        if (State.Outcome?.WinningSide == BattleSide.Attacker)
        {
            return (FormatOutcomeTitle(BattleSide.Attacker), FormatOutcome(BattleSide.Attacker), IconMoniker.Damage);
        }
        else
        {
            return (FormatOutcomeTitle(BattleSide.Defender), FormatOutcome(BattleSide.Defender), IconMoniker.Defend);
        }
    }

    private string FormatOutcomeTitle(BattleSide winningSide)
    {
        if (battle.ViewerSide == BattleViewerSide.None)
        {
            return Tr("Battle over");
        }
        else if (SelectOutcomeSide(winningSide) == winningSide)
        {
            return Tr("Victory");
        }
        else
        {
            return Tr("Defeat");
        }
    }

    private TextSpans FormatOutcome(BattleSide winningSide)
    {
        BattleSide side = SelectOutcomeSide(winningSide);
        bool isVictory = side == winningSide;
        BattleUnit? leader = FindLeader(side);

        if (leader is not null)
        {
            return FormatLeaderOutcome(leader, isVictory);
        }
        else if (side == BattleSide.Defender && fortificationName is not null)
        {
            return FormatFortificationOutcome(fortificationName, isVictory);
        }
        else
        {
            return TextSpans.Empty;
        }
    }

    private BattleSide SelectOutcomeSide(BattleSide winningSide)
    {
        return battle.ViewerSide switch
        {
            BattleViewerSide.Attacker => BattleSide.Attacker,
            BattleViewerSide.Defender => BattleSide.Defender,
            BattleViewerSide.None => winningSide
        };
    }

    private BattleUnit? FindLeader(BattleSide side)
    {
        UnitGrid<BattleUnit> grid = State.GetGrid(side);

        foreach ((BattleUnit unit, GridPosition _) in grid.GetUnits())
        {
            UnitDefinition definition = GameModel.Definitions.GetUnit(unit.DefinitionId);

            if (unit.SummonerId is null && definition.Leadership is > 0)
            {
                return unit;
            }
        }

        return null;
    }

    private TextSpans FormatLeaderOutcome(BattleUnit leader, bool isVictory)
    {
        string name = FormatUnitName(leader.Id);

        if (isVictory)
        {
            return FormatLeaderVictory(leader, name);
        }
        else
        {
            return FormatLeaderDefeat(leader, name);
        }
    }

    private static TextSpans FormatLeaderVictory(BattleUnit leader, string name)
    {
        if (!leader.IsAlive)
        {
            return TrSpans("{0:*} has fallen, but the battle is won.", name);
        }
        else if (IsRetreated(leader))
        {
            return TrSpans("{0:*} has fled, but the battle has been won.", name);
        }
        else
        {
            return TrSpans("{0:*} has won the battle.", name);
        }
    }

    private static TextSpans FormatLeaderDefeat(BattleUnit leader, string name)
    {
        if (!leader.IsAlive)
        {
            return TrSpans("{0:*} has fallen in battle.", name);
        }
        else if (IsRetreated(leader))
        {
            return TrSpans("{0:*} has fled from battle.", name);
        }
        else
        {
            return TrSpans("{0:*} has been defeated.", name);
        }
    }

    private static bool IsRetreated(BattleUnit unit)
    {
        return unit.StatusEffects.Any(effect => effect is BattleRetreatedStatusEffect);
    }

    private static TextSpans FormatFortificationOutcome(string name, bool isVictory)
    {
        if (isVictory)
        {
            return TrSpans("{0:*} holds!", name);
        }
        else
        {
            return TrSpans("{0:*} has fallen!", name);
        }
    }

    private string FormatUnitName(BattleUnitId id)
    {
        return UnitNameResolver.Resolve(State.GetUnit(id), GameModel.Definitions, GameModel.Texts);
    }

    private static int SelectBackgroundVariant(WorldPosition position)
    {
        return position.X + (5 * position.Y);
    }

    private static BattleEnvironment SelectEnvironment(IRandomProvider random)
    {
        BattleTimeOfDay[] timesOfDay = Enum.GetValues<BattleTimeOfDay>();
        BattleTimeOfDay timeOfDay = timesOfDay[random.Next(timesOfDay.Length)];
        bool rain = random.Next(2) != 0;
        bool storm = random.Next(2) != 0;

        return new BattleEnvironment(timeOfDay, rain, storm);
    }

    private static string? ResolveFortificationName(GameModel gameModel, WorldPosition position)
    {
        WorldPlan plan = WorldPlan.For(gameModel.Snapshot.WorldState);
        WorldFortification? fortification = plan.GetFortificationAt(position);

        if (fortification is not null)
        {
            return gameModel.Texts.GetScenarioText(fortification.NameTextId);
        }
        else
        {
            return null;
        }
    }

    private static AssetSpriteKey.BattleLocation ResolveBattleLocation(GameModel gameModel, BattleState state, WorldPosition position)
    {
        WorldState world = gameModel.Snapshot.WorldState;
        WorldPlan plan = WorldPlan.For(world);
        WorldMapCell mapCell = world.Map.GetCellAt(position);
        WorldFortification? fortification = plan.GetFortificationAt(position);

        if (fortification is not null)
        {
            return fortification switch
            {
                WorldCapital capital => new AssetSpriteKey.BattleLocation.Capital(world.Players[capital.OwnerId].Race),
                WorldCity => new AssetSpriteKey.BattleLocation.City(),
                WorldRuin => new AssetSpriteKey.BattleLocation.Ruin(),
                _ => throw new ArgumentOutOfRangeException(nameof(fortification), fortification, null)
            };
        }
        else if (mapCell.Terrain == WorldMapTerrain.Water)
        {
            if (IsBoatBattle(gameModel, state))
            {
                return new AssetSpriteKey.BattleLocation.Boat();
            }
            else
            {
                return new AssetSpriteKey.BattleLocation.Terrain(mapCell.Terrain);
            }
        }
        else
        {
            return new AssetSpriteKey.BattleLocation.Terrain(mapCell.Terrain);
        }
    }

    private static bool IsBoatBattle(GameModel gameModel, BattleState state)
    {
        foreach (BattleUnitId id in state.GetAllUnitIds())
        {
            BattleUnit unit = state.GetUnit(id);
            UnitDefinition definition = gameModel.Definitions.GetUnit(unit.DefinitionId);

            if (!definition.IsWaterOnly && !definition.MoveAbility.HasFlag(UnitMoveAbility.Flying))
            {
                return true;
            }
        }

        return false;
    }
}
