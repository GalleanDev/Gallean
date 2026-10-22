using Gallean.Common;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class ScenarioEventEffectStateVisitor :
        IScenarioEventEffectVisitor<ScenarioEventEffectStateVisitor.Context, WorldState>,
        IStaticInstance<ScenarioEventEffectStateVisitor>
    {
        public readonly record struct Context(
            WorldState State,
            IEngineContext EngineContext,
            WorldScenarioEventTriggeringEvent TriggeredEvent);

        public WorldState Visit(ScenarioEventPopupEffect effect, Context context)
        {
            return context.State;
        }

        public WorldState Visit(ScenarioEventWinEffect effect, Context context)
        {
            return context.State;
        }

        public WorldState Visit(ScenarioEventMoveStackEffect effect, Context context)
        {
            return context.State;
        }

        public WorldState Visit(ScenarioEventMoveBesideEffect effect, Context context)
        {
            return context.State;
        }

        public WorldState Visit(ScenarioEventBattleEffect effect, Context context)
        {
            return context.State;
        }

        public WorldState Visit(ScenarioEventCastSpellEffect effect, Context context)
        {
            return context.State;
        }

        public WorldState Visit(ScenarioEventCastSpellMapEffect effect, Context context)
        {
            return context.State;
        }

        public WorldState Visit(ScenarioEventCreateStackEffect effect, Context context)
        {
            return context.State;
        }

        public WorldState Visit(ScenarioEventRemoveStackEffect effect, Context context)
        {
            return context.State;
        }

        public WorldState Visit(ScenarioEventChangeOwnerEffect effect, Context context)
        {
            return context.State;
        }

        public WorldState Visit(ScenarioEventChangeOrderEffect effect, Context context)
        {
            return context.State;
        }

        public WorldState Visit(ScenarioEventEnableEventEffect effect, Context context)
        {
            WorldScenarioState scenarioState = context.State.ScenarioState with
            {
                EventStates = context.State.ScenarioState.EventStates.SetItem(effect.TargetEvent, effect.Enabled)
            };

            return context.State with { ScenarioState = scenarioState };
        }

        public WorldState Visit(ScenarioEventGiveItemEffect effect, Context context)
        {
            return context.State;
        }

        public WorldState Visit(ScenarioEventGiveSpellEffect effect, Context context)
        {
            return context.State;
        }

        public WorldState Visit(ScenarioEventDestroyItemEffect effect, Context context)
        {
            if (effect.TriggererOnly)
            {
                WorldStack? triggerer = ScenarioEventTriggererCalculator.FindStack(context.TriggeredEvent.Trigger, context.State);

                if (triggerer is null)
                {
                    return context.State;
                }

                return ScenarioItemDestructionCalculator.DestroyInStack(
                    context.State, effect.Item, triggerer.Id, context.EngineContext.Definitions);
            }
            else
            {
                return ScenarioItemDestructionCalculator.DestroyInWorld(context.State, effect.Item, context.EngineContext.Definitions);
            }
        }

        public WorldState Visit(ScenarioEventAllyEffect effect, Context context)
        {
            WorldRelation relation = DiplomacyCalculator.GetRelation(context.State, effect.Player1, effect.Player2);
            WorldRelation updatedRelation = relation with { Current = context.EngineContext.Globals.DiplomacyPeaceRelation };

            return context.State with { Relations = context.State.Relations.SetItem(relation.Id, updatedRelation) };
        }

        public WorldState Visit(ScenarioEventDiplomacyEffect effect, Context context)
        {
            WorldRelation relation = DiplomacyCalculator.GetRelation(context.State, effect.Player1, effect.Player2);
            int value = Math.Clamp(
                effect.Relation,
                DiplomacyCalculator.RelationMinimum,
                DiplomacyCalculator.RelationMaximum);

            WorldRelation updatedRelation = relation with
            {
                Current = value,
                PermanentWar = effect.PermanentWar ?? relation.PermanentWar
            };

            return context.State with { Relations = context.State.Relations.SetItem(relation.Id, updatedRelation) };
        }

        public WorldState Visit(ScenarioEventUnfogEffect effect, Context context)
        {
            return context.State;
        }

        public WorldState Visit(ScenarioEventRemoveMountainEffect effect, Context context)
        {
            return context.State;
        }

        public WorldState Visit(ScenarioEventRemoveLandmarkEffect effect, Context context)
        {
            return context.State with
            {
                Landmarks = context.State.Landmarks.Remove(effect.Landmark)
            };
        }

        public WorldState Visit(ScenarioEventChangeObjectiveEffect effect, Context context)
        {
            return context.State with
            {
                ScenarioState = context.State.ScenarioState with { ObjectiveTextId = effect.TextId }
            };
        }

        public WorldState Visit(ScenarioEventChangeLandmarkEffect effect, Context context)
        {
            if (context.State.Landmarks.TryGetValue(effect.Landmark, out WorldLandmark? landmark))
            {
                WorldLandmarkDefinition definition = context.EngineContext.Definitions.GetLandmark(effect.LandmarkType);
                WorldLandmark updatedLandmark = landmark with
                {
                    DefinitionId = definition.Id,
                    Footprint = new WorldFootprint(landmark.Footprint.TopLeft, definition.Width, definition.Height)
                };

                return context.State with
                {
                    Landmarks = context.State.Landmarks.SetItem(landmark.Id, updatedLandmark)
                };
            }
            else
            {
                return context.State;
            }
        }

        public WorldState Visit(ScenarioEventChangeTerrainEffect effect, Context context)
        {
            ScenarioLocationDefinition location = context.EngineContext.Scenario.GetLocation(effect.Location);

            WorldMap map = TerrainTransformCalculator.TransformArea(
                context.State,
                location.Position,
                effect.AreaSize,
                effect.Terrain);

            return context.State with { Map = map };
        }

        public WorldState Visit(ScenarioEventModifyVariableEffect effect, Context context)
        {
            int value = ScenarioVariableCalculator.GetValue(effect.Variable, context.State, context.EngineContext.Scenario);
            int modifiedValue = ScenarioVariableCalculator.Modify(value, effect.Operation, effect.Value);
            WorldScenarioState scenarioState = context.State.ScenarioState with
            {
                Variables = context.State.ScenarioState.Variables.SetItem(effect.Variable, modifiedValue)
            };

            return context.State with { ScenarioState = scenarioState };
        }
    }
}
