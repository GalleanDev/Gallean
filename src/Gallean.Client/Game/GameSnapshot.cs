namespace Gallean.Client;

internal sealed record GameSnapshot(
    WorldState WorldState,
    BattleState? BattleState,
    int ActiveActions,
    WorldPlayerId ViewPlayerId)
{
    public bool IsBusy => ActiveActions > 0;

    public bool IsViewPlayerTurn => WorldState.TurnInfo.CurrentPlayerId == ViewPlayerId;

    public bool IsSaveAllowed => !IsBusy && IsViewPlayerTurn && BattleState is null;

    public bool IsGameEnded => WorldState.ScenarioState.IsEnded;
}
