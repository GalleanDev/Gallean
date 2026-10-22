namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleRoundEndedEvent ev, BattleState state)
    {
        return state;
    }
}
