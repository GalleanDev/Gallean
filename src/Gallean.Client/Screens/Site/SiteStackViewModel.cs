using CommunityToolkit.Mvvm.ComponentModel;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class SiteStackViewModel(GameModel gameModel, UnitGridSelectionViewModel selection) : ObservableObject
{
    public UnitGridViewModel UnitGrid { get; } = new(gameModel, selection, isFacingLeft: false);

    [ObservableProperty]
    public partial AssetSpriteKey.UnitPortrait? LeaderPortraitKey { get; private set; }

    [ObservableProperty]
    public partial string LeaderName { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial TextSpans Summary { get; private set; } = TextSpans.Empty;

    public void Update(WorldStack stack, bool showsPurchaseHints, IReadOnlySet<GridPosition>? highlight)
    {
        UnitGrid.Update(stack.Units, showsPurchaseHints, highlight);

        (WorldUnit Leader, GridPosition Position, UnitDefinition Definition) leader = stack.GetLeader(gameModel.Definitions);
        StackStats stats = gameModel.Stats.GetWorldStackStats(stack);
        int unitCount = stack.Units.GetUnits().Count();

        LeaderPortraitKey = new AssetSpriteKey.UnitPortrait(leader.Leader.DefinitionId);
        LeaderName = UnitNameResolver.Resolve(leader.Leader, gameModel.Definitions, gameModel.Texts);
        Summary = TrSpans("Move: {0:*} / {1:*}\nUnits: {2:*} / {3:*}", stack.MovementPoints, stats.MaxMovementPoints, unitCount, GridPosition.Count);
    }
}
