using Gallean.Common;

namespace Gallean.Engine;

internal sealed class WorldAiSpellcasting(IEngineContext engine)
{
    public WorldCastSpellCommand? FindWeakenCast(WorldState state, WorldPlayerId playerId, WorldStack target)
    {
        WorldPlayer player = state.Players[playerId];
        List<WorldSpellDefinition> spells = [];

        foreach (WorldSpellDefinitionId spellId in player.Spellbook.Acquired)
        {
            WorldSpellDefinition spell = engine.Definitions.GetSpell(spellId);

            if (spell.AreaRadius is 0 &&
                GetEffectType(spell) is WorldAiSpellEffectType.Weaken)
            {
                spells.Add(spell);
            }
        }

        spells.Sort(CompareLevel);

        if (IsEnemyCastAllowed(player))
        {
            return FindCast(state, spells, [target]);
        }
        else
        {
            return null;
        }
    }

    public WorldCastSpellCommand? FindCast(WorldState state, List<WorldSpellDefinition> spells, IEnumerable<WorldStack> targets)
    {
        foreach (WorldSpellDefinition spell in spells)
        {
            foreach (WorldStack target in targets)
            {
                WorldCastSpellCommand command = new(spell.Id, new WorldSpellTarget(target.Position, 1));
                WorldCommandResult result = command.Execute(state, engine);

                if (!result.Failed)
                {
                    return command;
                }
            }
        }

        return null;
    }

    public bool IsEnemyCastAllowed(WorldPlayer player)
    {
        foreach (WorldSpellDefinitionId spellId in player.TurnActions.SpellCasts.Keys)
        {
            WorldSpellDefinition spell = engine.Definitions.GetSpell(spellId);

            if (spell.AreaRadius is 0 &&
                GetEffectType(spell) is WorldAiSpellEffectType.Attack or WorldAiSpellEffectType.Summon or WorldAiSpellEffectType.Weaken)
            {
                return false;
            }
        }

        return true;
    }

    private static WorldAiSpellEffectType? GetEffectType(WorldSpellDefinition spell)
    {
        return spell.Effect.Accept(WorldAiSpellEffectTypeVisitor.Instance, Empty.Value);
    }

    private static int CompareLevel(WorldSpellDefinition left, WorldSpellDefinition right)
    {
        return right.Level.CompareTo(left.Level);
    }
}
