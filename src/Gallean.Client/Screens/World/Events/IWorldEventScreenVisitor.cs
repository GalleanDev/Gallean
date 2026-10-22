namespace Gallean.Client;

internal interface IWorldEventScreenVisitor<out TResult>
{
    TResult Visit(WorldEventAction.Loading action);
    TResult Visit(WorldEventAction.HotseatTurn action);
    TResult Visit(WorldEventAction.ScenarioPopup action);
    TResult Visit(WorldEventAction.RuinLoot action);
    TResult Visit(WorldEventAction.ItemsReceive action);
    TResult Visit(WorldEventAction.SpellsReceive action);
    TResult Visit(WorldEventAction.LeaderUpgrade action);
    TResult Visit(WorldEventAction.DiplomacyProposal action);
    TResult Visit(WorldEventAction.ThiefAction action);
    TResult Visit(WorldEventAction.ThiefActionResult action);
    TResult Visit(WorldEventAction.SiteEnter action);
}
