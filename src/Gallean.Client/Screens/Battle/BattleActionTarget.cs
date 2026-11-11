namespace Gallean.Client;

internal closed record BattleActionTarget
{
    public sealed record Unit(IReadOnlyCollection<BattleUnitId> Units) : BattleActionTarget;

    public sealed record Summon(IReadOnlyCollection<BattleGridTile> Tiles) : BattleActionTarget;
}
