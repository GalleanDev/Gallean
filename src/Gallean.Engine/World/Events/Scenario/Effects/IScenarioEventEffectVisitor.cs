namespace Gallean.Engine;

public interface IScenarioEventEffectVisitor<in TContext, out TResult>
{
    TResult Visit(ScenarioEventPopupEffect effect, TContext context);
    TResult Visit(ScenarioEventWinEffect effect, TContext context);
    TResult Visit(ScenarioEventMoveStackEffect effect, TContext context);
    TResult Visit(ScenarioEventMoveBesideEffect effect, TContext context);
    TResult Visit(ScenarioEventBattleEffect effect, TContext context);
    TResult Visit(ScenarioEventCastSpellEffect effect, TContext context);
    TResult Visit(ScenarioEventCastSpellMapEffect effect, TContext context);
    TResult Visit(ScenarioEventCreateStackEffect effect, TContext context);
    TResult Visit(ScenarioEventRemoveStackEffect effect, TContext context);
    TResult Visit(ScenarioEventChangeOwnerEffect effect, TContext context);
    TResult Visit(ScenarioEventChangeOrderEffect effect, TContext context);
    TResult Visit(ScenarioEventEnableEventEffect effect, TContext context);
    TResult Visit(ScenarioEventGiveItemEffect effect, TContext context);
    TResult Visit(ScenarioEventGiveSpellEffect effect, TContext context);
    TResult Visit(ScenarioEventDestroyItemEffect effect, TContext context);
    TResult Visit(ScenarioEventAllyEffect effect, TContext context);
    TResult Visit(ScenarioEventDiplomacyEffect effect, TContext context);
    TResult Visit(ScenarioEventUnfogEffect effect, TContext context);
    TResult Visit(ScenarioEventRemoveMountainEffect effect, TContext context);
    TResult Visit(ScenarioEventRemoveLandmarkEffect effect, TContext context);
    TResult Visit(ScenarioEventChangeObjectiveEffect effect, TContext context);
    TResult Visit(ScenarioEventChangeLandmarkEffect effect, TContext context);
    TResult Visit(ScenarioEventChangeTerrainEffect effect, TContext context);
    TResult Visit(ScenarioEventModifyVariableEffect effect, TContext context);
}
