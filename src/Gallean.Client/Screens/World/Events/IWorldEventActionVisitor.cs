namespace Gallean.Client;

internal interface IWorldEventActionVisitor<out TResult>
{
    TResult Visit(WorldEventAction.OneShot action);
    TResult Visit(WorldEventAction.AutoSave action);
    TResult Visit(WorldEventAction.TurnStart action);
    TResult Visit(WorldEventAction.ObjectiveChange action);
    TResult Visit(WorldEventAction.FortificationExit action);
    TResult Visit(WorldEventAction.StackExhaust action);
    TResult Visit(WorldEventAction.Animation action);
    TResult Visit(WorldEventAction.Battle action);
    TResult Visit(WorldEventAction.Screen action);
}
