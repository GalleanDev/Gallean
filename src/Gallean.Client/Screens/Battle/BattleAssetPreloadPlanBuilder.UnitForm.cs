namespace Gallean.Client;

internal sealed partial class BattleAssetPreloadPlanBuilder
{
    private sealed record UnitForm(UnitDefinitionId DefinitionId, BattleSide Facing);
}
