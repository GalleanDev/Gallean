using Gallean.Common;

namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed record CastSpellAction(WorldAiContext Context, int Priority) : AbstractEconomyAction(Context, Priority)
    {
        private const int CityThreatRange = 7;

        public override WorldAiActionPlan? Plan(WorldState state, WorldPlayerId playerId, WorldAiMemory memory)
        {
            WorldPlayer player = state.Players[playerId];
            List<WorldSpellDefinition> targetedSpells = [];
            List<WorldSpellDefinition> globalSpells = [];

            foreach (WorldSpellDefinitionId spellId in player.Spellbook.Acquired)
            {
                WorldSpellDefinition spell = Context.Engine.Definitions.GetSpell(spellId);

                if (spell.AreaRadius is 0)
                {
                    targetedSpells.Add(spell);
                }

                if (spell.AreaRadius is null)
                {
                    globalSpells.Add(spell);
                }
            }

            targetedSpells.Sort(CompareSpellLevel);
            globalSpells.Sort(CompareSpellLevel);

            WorldCastSpellCommand? command = FindTargetedCast(state, playerId, targetedSpells);

            if (command is null && IsGlobalCastAllowed(player, globalSpells))
            {
                command = FindGlobalCast(state, playerId, globalSpells);
            }

            if (command is null)
            {
                return null;
            }

            return MakePlan(MaxScore, command, memory);
        }

        private WorldCastSpellCommand? FindTargetedCast(WorldState state, WorldPlayerId playerId, List<WorldSpellDefinition> spells)
        {
            foreach (WorldSpellDefinition spell in spells)
            {
                WorldAiSpellEffectType? type = spell.Effect.Accept(WorldAiSpellEffectTypeVisitor.Instance, Empty.Value);

                IEnumerable<WorldStack> targets = type switch
                {
                    WorldAiSpellEffectType.Summon => EnumerateCityThreats(state, playerId),
                    WorldAiSpellEffectType.Attack => EnumerateCityThreats(state, playerId),
                    WorldAiSpellEffectType.Heal => EnumerateWoundedStacks(state, playerId),
                    WorldAiSpellEffectType.Boost => [],
                    WorldAiSpellEffectType.Invisibility => [],
                    null => []
                };

                foreach (WorldStack target in targets)
                {
                    WorldCastSpellCommand command = new(spell.Id, new WorldSpellTarget(target.Position, 1));
                    WorldCommandResult result = command.Execute(state, Context.Engine);

                    if (!result.Failed)
                    {
                        return command;
                    }
                }
            }

            return null;
        }

        private bool IsGlobalCastAllowed(WorldPlayer player, List<WorldSpellDefinition> globalSpells)
        {
            GlobalVariables globals = Context.Engine.Globals;
            WorldResourceSet resources = player.Resources;

            return Context.Engine.Scenario.Definition.SuggestedLevel >= globals.AiGlobalSpellMinSuggestedLevel &&
                   resources.InfernalMana > globals.AiGlobalSpellMinMana &&
                   resources.LifeMana > globals.AiGlobalSpellMinMana &&
                   resources.DeathMana > globals.AiGlobalSpellMinMana &&
                   resources.RuneMana > globals.AiGlobalSpellMinMana &&
                   !globalSpells.Any(spell => player.TurnActions.SpellCasts.ContainsKey(spell.Id));
        }

        private WorldCastSpellCommand? FindGlobalCast(WorldState state, WorldPlayerId playerId, List<WorldSpellDefinition> spells)
        {
            List<WorldStack> stacks = [.. EnumerateOutsideStacks(state, playerId)];

            foreach (WorldSpellDefinition spell in spells)
            {
                WorldAiSpellEffectType? type = spell.Effect.Accept(WorldAiSpellEffectTypeVisitor.Instance, Empty.Value);

                bool castSpell = type switch
                {
                    WorldAiSpellEffectType.Heal => stacks.Any(HasDamagedUnit),
                    WorldAiSpellEffectType.Boost => stacks.Count >= Context.Engine.Globals.AiGlobalSpellMinStacks,
                    WorldAiSpellEffectType.Invisibility => stacks.Count >= Context.Engine.Globals.AiGlobalSpellMinStacks,
                    WorldAiSpellEffectType.Summon => false,
                    WorldAiSpellEffectType.Attack => false,
                    null => false
                };

                if (castSpell)
                {
                    WorldCastSpellCommand command = new(spell.Id, Target: null);
                    WorldCommandResult result = command.Execute(state, Context.Engine);

                    if (!result.Failed)
                    {
                        return command;
                    }
                }
            }

            return null;
        }

        private static int CompareSpellLevel(WorldSpellDefinition left, WorldSpellDefinition right)
        {
            return right.Level.CompareTo(left.Level);
        }

        private static IEnumerable<WorldStack> EnumerateCityThreats(WorldState state, WorldPlayerId playerId)
        {
            WorldPlan plan = WorldPlan.For(state);

            foreach (WorldFortification fortification in state.Fortifications.Values)
            {
                WorldPosition entrance = FortificationCalculator.GetEntrance(fortification.Footprint);

                if (FortificationCalculator.GetOwner(fortification) == playerId && plan.GetStackAt(entrance) is null)
                {
                    foreach (WorldStack stack in state.Stacks.Values)
                    {
                        if (DiplomacyCalculator.IsHostile(state, playerId, stack.OwnerId) &&
                            entrance.GetDistance(stack.Position) <= CityThreatRange)
                        {
                            yield return stack;
                        }
                    }
                }
            }
        }

        private IEnumerable<WorldStack> EnumerateWoundedStacks(WorldState state, WorldPlayerId playerId)
        {
            foreach (WorldStack stack in state.Stacks.Values)
            {
                if (stack.OwnerId == playerId && IsStackWounded(stack))
                {
                    yield return stack;
                }
            }
        }

        private static IEnumerable<WorldStack> EnumerateOutsideStacks(WorldState state, WorldPlayerId playerId)
        {
            WorldPlan plan = WorldPlan.For(state);

            foreach (WorldStack stack in state.Stacks.Values)
            {
                if (stack.OwnerId == playerId && plan.GetFortificationAt(stack.Position) is null)
                {
                    yield return stack;
                }
            }
        }

        private bool IsStackWounded(WorldStack stack)
        {
            int hitpoints = 0;
            int maxHitpoints = 0;

            foreach ((WorldUnit unit, GridPosition _) in stack.Units.GetUnits())
            {
                UnitStats stats = Context.Engine.UnitStats.GetWorldUnitStats(unit);

                hitpoints += unit.Hitpoints;
                maxHitpoints += stats.MaxHitpoints;
            }

            return hitpoints * 2 <= maxHitpoints;
        }

        private bool HasDamagedUnit(WorldStack stack)
        {
            foreach ((WorldUnit unit, GridPosition _) in stack.Units.GetUnits())
            {
                UnitStats stats = Context.Engine.UnitStats.GetWorldUnitStats(unit);

                if (unit.Hitpoints < stats.MaxHitpoints)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
