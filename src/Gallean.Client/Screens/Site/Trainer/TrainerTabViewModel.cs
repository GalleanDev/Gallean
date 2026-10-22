using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class TrainerTabViewModel
    : ManagementTabViewModel, IUnitGridSelectionProvider, IDragDropProvider, IDragDropHost
{
    private readonly ManagementScreenViewModel screen;
    private readonly WorldStackId stackId;
    private readonly UnitGridSelectionViewModel unitSelection;
    private WorldStack? currentStack;

    public TrainerTabViewModel(
        GameModel gameModel,
        GameViewModel gameViewModel,
        ManagementScreenViewModel screen,
        WorldStackId stackId)
        : base(gameModel, gameViewModel)
    {
        this.screen = screen;
        this.stackId = stackId;

        unitSelection = new UnitGridSelectionViewModel(gameModel, this);
        Stack = new SiteStackViewModel(gameModel, unitSelection);
        DragDrop = new DragDropViewModel(gameModel, this);

        RefreshView();

        RegisterSnapshotGatedCommands(TrainCommand);
    }

    public override string TabHeader => Tr("Trainer");
    public override AssetSpriteKey.SiteImage SiteImageKey { get; } = new(AssetAnimationKey.SiteType.Trainer);

    public SiteStackViewModel Stack { get; }
    public DragDropViewModel DragDrop { get; }

    public string OfferHint => Tr("Select a unit to train it.");

    [ObservableProperty]
    public partial string SiteTitle { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string SiteGreeting { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial TrainerInspectedUnitViewModel? InspectedUnit { get; private set; }

    [ObservableProperty]
    public partial SiteOfferViewModel? Offer { get; private set; }

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

        return await TryDispatchWorldActionAsync(
            new WorldRepositionUnitCommand(new WorldUnitGroup.Stack(currentStack.Id), unit.Id, target.Position));
    }

    async Task IDragDropProvider.DropAsync(IDragSourceReference source, IDragTargetReference target)
    {
        if (source is UnitGridSlotReference sourceSlot && target is UnitGridSlotReference targetSlot)
        {
            await unitSelection.DropUnitAsync(sourceSlot, targetSlot);
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

    [RelayCommand(CanExecute = nameof(CanTrain))]
    private async Task TrainAsync()
    {
        UnitGridSlotReference? selected = unitSelection.Selected;

        if (currentStack is not null && selected is not null)
        {
            WorldUnit? unit = currentStack.Units[selected.Value.Position];

            if (unit is not null)
            {
                string name = UnitNameResolver.Resolve(unit, GameModel.Definitions, GameModel.Texts);

                if (await TryDispatchWorldActionAsync(new WorldTrainUnitCommand(currentStack.Id, unit.Id)))
                {
                    Game.ShowInfo(IconMoniker.Experience, Tr("Unit trained"), name);
                }

                RefreshView();
            }
        }
    }

    private bool CanTrain()
    {
        if (IsBusy)
        {
            return false;
        }

        return Offer is { Status.IsAvailable: true };
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

    private void RefreshView()
    {
        WorldPlan plan = WorldPlan.For(WorldState);

        if (!WorldState.Stacks.TryGetValue(stackId, out WorldStack? stack) ||
            stack.OwnerId != ViewPlayerId ||
            plan.GetSiteAt(stack.Position) is not WorldTrainerSite site)
        {
            screen.Close();

            return;
        }

        currentStack = stack;

        WorldGetTrainerInfoCommand.Result info = ResolveTrainerInfo();

        SiteTitle = GameModel.Texts.GetScenarioText(site.TitleTextId);
        SiteGreeting = GameModel.Texts.GetScenarioText(site.GreetingTextId);
        Stack.Update(stack, showsPurchaseHints: false, BuildUnitHighlight(stack, info));

        UpdateTrainingDetails(stack, info);
        TrainCommand.NotifyCanExecuteChanged();
    }

    private WorldGetTrainerInfoCommand.Result ResolveTrainerInfo()
    {
        ICommandResult<WorldCommandError, WorldEvent, WorldGetTrainerInfoCommand.Result> result =
            GameModel.World.DryRunCommand(new WorldGetTrainerInfoCommand(stackId));

        if (result.Failed)
        {
            return new WorldGetTrainerInfoCommand.Result([]);
        }
        else
        {
            return result.Data;
        }
    }

    private IReadOnlySet<GridPosition> BuildUnitHighlight(WorldStack stack, WorldGetTrainerInfoCommand.Result info)
    {
        if (DragDrop.Dragged is UnitGridSlotReference source)
        {
            return BuildMoveHighlight(stack, source);
        }
        else
        {
            return BuildTrainingHighlight(stack, info);
        }
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

    private static IReadOnlySet<GridPosition> BuildTrainingHighlight(WorldStack stack, WorldGetTrainerInfoCommand.Result info)
    {
        HashSet<GridPosition> result = [];

        foreach ((WorldUnit unit, GridPosition position) in stack.Units.GetUnits())
        {
            WorldGetTrainerInfoCommand.TrainingUnit? trainingUnit = FindTrainingUnit(info, unit.Id);

            if (trainingUnit is not null && trainingUnit.Error is null)
            {
                result.Add(position);
            }
        }

        return result;
    }

    private static WorldGetTrainerInfoCommand.TrainingUnit? FindTrainingUnit(
        WorldGetTrainerInfoCommand.Result info,
        WorldUnitId unitId)
    {
        foreach (WorldGetTrainerInfoCommand.TrainingUnit trainingUnit in info.Units)
        {
            if (trainingUnit.UnitId == unitId)
            {
                return trainingUnit;
            }
        }

        return null;
    }

    private void UpdateTrainingDetails(WorldStack stack, WorldGetTrainerInfoCommand.Result info)
    {
        if (unitSelection.Selected is not { } selectedSlot)
        {
            ClearTrainingDetails();

            return;
        }

        WorldUnit? selectedUnit = stack.Units[selectedSlot.Position];

        if (selectedUnit is null)
        {
            ClearTrainingDetails();

            return;
        }

        if (FindTrainingUnit(info, selectedUnit.Id) is { } trainingUnit)
        {
            InspectedUnit = BuildInspectedUnit(selectedUnit, trainingUnit);
            Offer = BuildOffer(selectedUnit, trainingUnit);
        }
        else
        {
            ClearTrainingDetails();
        }
    }

    private void ClearTrainingDetails()
    {
        InspectedUnit = null;
        Offer = null;
    }

    private TrainerInspectedUnitViewModel BuildInspectedUnit(WorldUnit unit, WorldGetTrainerInfoCommand.TrainingUnit trainingUnit)
    {
        UnitExperience experience = GameModel.Stats.GetWorldUnitExperience(unit);
        int trainableExperience = GetTrainableExperience(trainingUnit);

        return new TrainerInspectedUnitViewModel(
            new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(unit.DefinitionId)),
            UnitNameResolver.Resolve(unit, GameModel.Definitions, GameModel.Texts),
            ResolveTrainingGain(trainingUnit, trainableExperience),
            ResolveBarExperience(experience),
            experience.NextLevel,
            trainableExperience,
            FormatExperience(experience, trainableExperience),
            experience.IsMaximumLevel,
            experience.IsPendingUpgrade);
    }

    private SiteOfferViewModel BuildOffer(WorldUnit unit, WorldGetTrainerInfoCommand.TrainingUnit trainingUnit)
    {
        int experience = GetTrainableExperience(trainingUnit);
        string? unavailableReason;

        if (trainingUnit.Error is { } error)
        {
            unavailableReason = FormatUnavailability(error, trainingUnit.CostPerExperience);
        }
        else
        {
            unavailableReason = null;
        }

        WorldPlayer owner = WorldState.Players[ViewPlayerId];
        UnitDetailsViewModel details = UnitDetailsViewModel.CreateFromWorld(
            unit,
            owner,
            GameModel.Definitions,
            GameModel.Texts,
            GameModel.Stats);

        WorldResourceSet cost = MultiplyCost(trainingUnit.CostPerExperience, experience);

        return new SiteOfferViewModel(
            new IconViewModel.Unit(new AssetSpriteKey.UnitPortrait(unit.DefinitionId)),
            details,
            UnitNameResolver.Resolve(unit, GameModel.Definitions, GameModel.Texts),
            TrSpans("Level {0:*}", unit.Level),
            string.Empty,
            Tr("Train"),
            new OfferStatusViewModel(new OfferPriceViewModel(cost, WorldState.Players[ViewPlayerId].Resources), unavailableReason),
            TrainCommand);
    }

    private static WorldResourceSet MultiplyCost(WorldResourceSet cost, int factor)
    {
        return new WorldResourceSet(
            cost.Gold * factor,
            cost.LifeMana * factor,
            cost.DeathMana * factor,
            cost.RuneMana * factor,
            cost.InfernalMana * factor,
            cost.GroveMana * factor);
    }

    private static string ResolveTrainingGain(WorldGetTrainerInfoCommand.TrainingUnit trainingUnit, int experience)
    {
        if (trainingUnit.Error is not null)
        {
            return string.Empty;
        }
        else if (experience < trainingUnit.MaxExperience - trainingUnit.CurrentExperience)
        {
            return Tr("Gains {0} XP, the most you can afford.", experience);
        }
        else
        {
            return Tr("Gains {0} XP, enough to reach the next level.", experience);
        }
    }

    private static int ResolveBarExperience(UnitExperience experience)
    {
        if (experience.IsMaximumLevel)
        {
            return experience.NextLevel;
        }
        else
        {
            return experience.Current;
        }
    }

    private static string FormatExperience(UnitExperience experience, int trainableExperience)
    {
        if (experience.IsMaximumLevel)
        {
            return Tr("Max");
        }
        else if (trainableExperience > 0)
        {
            return string.Create(CultureInfo.CurrentCulture, $"{experience.Current}/{experience.NextLevel} +{trainableExperience}");
        }
        else
        {
            return string.Create(CultureInfo.CurrentCulture, $"{experience.Current}/{experience.NextLevel}");
        }
    }

    private static int GetTrainableExperience(WorldGetTrainerInfoCommand.TrainingUnit trainingUnit)
    {
        if (trainingUnit.Error is null)
        {
            return Math.Min(trainingUnit.MaxExperience - trainingUnit.CurrentExperience, trainingUnit.AffordableExperience);
        }
        else
        {
            return 0;
        }
    }

    private static string FormatUnavailability(WorldCommandError error, WorldResourceSet costPerExperience)
    {
        return error switch
        {
            WorldCommandError.InsufficientResources =>
                Tr("Training costs {0} gold per experience point. You need more gold.", costPerExperience.Gold),
            WorldCommandError.UnitAlreadyTrained => Tr("This unit is already trained and will upgrade at the end of its next battle."),
            WorldCommandError.UnitDead => Tr("The unit is dead and cannot be trained."),
            WorldCommandError.UnitFullyTrained =>
                Tr("This unit has reached its maximum experience level and can no longer benefit from training."),
            WorldCommandError.UnitTrainingForbidden => Tr("This unit cannot be trained."),
            _ => WorldCommandErrorFormatter.Format(error)
        };
    }
}
