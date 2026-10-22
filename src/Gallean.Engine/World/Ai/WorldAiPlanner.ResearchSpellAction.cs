using Gallean.Common;

namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed record ResearchSpellAction(WorldAiContext Context, int Priority) : AbstractEconomyAction(Context, Priority)
    {
        private const int ResearchTurnInterval = 5;
        private const int MaxResearchLevel = 4;
        private const int MaxKnownSpells = 12;

        private static readonly WorldAiSpellEffectType[] ResearchTypes =
        [
            WorldAiSpellEffectType.Summon,
            WorldAiSpellEffectType.Attack,
            WorldAiSpellEffectType.Heal
        ];

        public override WorldAiActionPlan? Plan(WorldState state, WorldPlayerId playerId, WorldAiMemory memory)
        {
            if (state.TurnInfo.TurnNumber % ResearchTurnInterval != 0)
            {
                return null;
            }

            WorldPlayer player = state.Players[playerId];

            if (player.Spellbook.Acquired.Count >= MaxKnownSpells)
            {
                return null;
            }

            for (int level = 1; level <= MaxResearchLevel; level++)
            {
                foreach (WorldAiSpellEffectType type in ResearchTypes)
                {
                    foreach (WorldSpellDefinitionId spellId in player.Spellbook.Available)
                    {
                        WorldSpellDefinition spell = Context.Engine.Definitions.GetSpell(spellId);
                        WorldAiSpellEffectType? spellType = spell.Effect.Accept(WorldAiSpellEffectTypeVisitor.Instance, Empty.Value);

                        if (IsResearchable(spell.AiType) && spell.Level == level && spellType == type)
                        {
                            WorldResearchSpellCommand command = new(playerId, spellId);
                            WorldCommandResult result = command.Execute(state, Context.Engine);

                            if (!result.Failed)
                            {
                                return MakePlan(MaxScore, command, memory);
                            }
                        }
                    }
                }
            }

            return null;
        }

        private static bool IsResearchable(WorldAiSpellType type)
        {
            return type switch
            {
                WorldAiSpellType.All => true,
                WorldAiSpellType.None => false,
                WorldAiSpellType.Human => true,
                WorldAiSpellType.Undead => true,
                WorldAiSpellType.Heretic => true,
                WorldAiSpellType.Dwarf => true
            };
        }
    }
}
