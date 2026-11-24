using CommunityToolkit.Mvvm.ComponentModel;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class ThiefStealScreenViewModel : AbstractGameScreenViewModel
{
    private readonly WorldStackId stackId;
    private readonly WorldPosition targetPosition;
    private readonly Func<WorldThiefAction, Task> stealAction;

    public ThiefStealScreenViewModel(
        GameModel gameModel,
        GameViewModel gameViewModel,
        WorldStackId stackId,
        WorldPosition targetPosition,
        Func<WorldThiefAction, Task> stealAction)
        : base(gameModel, gameViewModel)
    {
        this.stackId = stackId;
        this.targetPosition = targetPosition;
        this.stealAction = stealAction;

        RefreshView();
    }

    [ObservableProperty]
    public partial IReadOnlyList<ThiefStealItemViewModel> Targets { get; private set; } = [];

    public async Task StealAsync(ThiefStealItemViewModel item)
    {
        if (IsBusy || !item.Status.IsAvailable)
        {
            return;
        }

        CloseSelf();

        await stealAction(item.Action);
    }

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }

    protected override void OnSnapshotChanged(GameSnapshot current)
    {
        RefreshView();
    }

    private void RefreshView()
    {
        Targets = BuildTargets(ResolveStealInfo());
    }

    private WorldGetThiefStealInfoCommand.Result ResolveStealInfo()
    {
        ICommandResult<WorldCommandError, WorldEvent, WorldGetThiefStealInfoCommand.Result> result =
            GameModel.World.DryRunCommand(new WorldGetThiefStealInfoCommand(stackId, targetPosition));

        if (result.Failed)
        {
            return new WorldGetThiefStealInfoCommand.Result([], []);
        }
        else
        {
            return result.Data;
        }
    }

    private IReadOnlyList<ThiefStealItemViewModel> BuildTargets(WorldGetThiefStealInfoCommand.Result info)
    {
        List<(WorldResourceSet Cost, int Index, ThiefStealItemViewModel Target)> targets = [];

        foreach (WorldGetThiefStealInfoCommand.Item item in info.Items)
        {
            ItemDefinition definition = GameModel.Definitions.GetItem(item.Action.ItemId);
            IconViewModel.Item icon = new(ItemIconResolver.Resolve(GameModel.Definitions, item.Action.ItemId));

            string name = GameModel.Texts.GetGlobalText(definition.NameTextId).Text;
            string description = GameModel.Texts.GetGlobalText(definition.DescriptionTextId).Text;
            OfferPriceViewModel? cost;

            if (definition.Value != WorldResourceSet.Empty)
            {
                cost = new OfferPriceViewModel(definition.Value);
            }
            else
            {
                cost = null;
            }

            string? unavailableReason;

            if (item.Error is { } error)
            {
                unavailableReason = WorldCommandErrorFormatter.Format(error);
            }
            else
            {
                unavailableReason = null;
            }

            targets.Add((definition.Value, targets.Count, new ThiefStealItemViewModel(this, item.Action, icon, name, description, cost, castingCost: null, unavailableReason)));
        }

        foreach (WorldGetThiefStealInfoCommand.Spell spell in info.Spells)
        {
            WorldSpellDefinition definition = GameModel.Definitions.GetSpell(spell.Action.SpellId);
            IconViewModel.Spell icon = SpellIconResolver.Resolve(GameModel.Definitions, spell.Action.SpellId);

            string name = GameModel.Texts.GetGlobalText(definition.NameTextId).Text;
            string description = GameModel.Texts.GetGlobalText(definition.DescriptionTextId).Text;
            OfferPriceViewModel castingCost = new(definition.CastingCost);
            string? unavailableReason;

            if (spell.Error is { } error)
            {
                unavailableReason = WorldCommandErrorFormatter.Format(error);
            }
            else
            {
                unavailableReason = null;
            }

            targets.Add((definition.CastingCost, targets.Count, new ThiefStealItemViewModel(this, spell.Action, icon, name, description, cost: null, castingCost, unavailableReason)));
        }

        targets.Sort(CompareTargets);

        List<ThiefStealItemViewModel> result = [];

        foreach ((WorldResourceSet _, int _, ThiefStealItemViewModel target) in targets)
        {
            result.Add(target);
        }

        return result;
    }

    private static int CompareTargets(
        (WorldResourceSet Cost, int Index, ThiefStealItemViewModel Target) left,
        (WorldResourceSet Cost, int Index, ThiefStealItemViewModel Target) right)
    {
        int rv;

        rv = right.Target.Status.IsAvailable.CompareTo(left.Target.Status.IsAvailable);
        if (rv != 0)
        {
            return rv;
        }

        rv = OfferPriceComparer.Descending.Compare(left.Cost, right.Cost);
        if (rv != 0)
        {
            return rv;
        }

        rv = left.Index.CompareTo(right.Index);
        if (rv != 0)
        {
            return rv;
        }

        return 0;
    }
}
