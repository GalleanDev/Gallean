using Gallean.Common;

namespace Gallean.Engine;

internal sealed class WorldAiSpellcasting(IEngineContext engine)
{
    public WorldCastSpellCommand? FindAttackCast(WorldState state, WorldStack stack, WorldStack target, WorldAiEstimator.PowerEstimate power)
    {
        WorldPlayer player = state.Players[stack.OwnerId];
        List<WorldSpellDefinition> weakenSpells = [];
        List<WorldSpellDefinition> summonSpells = [];

        foreach (WorldSpellDefinitionId spellId in player.Spellbook.Acquired)
        {
            WorldSpellDefinition spell = engine.Definitions.GetSpell(spellId);

            if (spell.AreaRadius is 0)
            {
                WorldAiSpellEffectType? type = GetEffectType(spell);

                if (type is WorldAiSpellEffectType.Weaken)
                {
                    weakenSpells.Add(spell);
                }
                else if (type is WorldAiSpellEffectType.Summon)
                {
                    summonSpells.Add(spell);
                }
            }
        }

        weakenSpells.Sort(CompareLevel);
        summonSpells.Sort(CompareLevel);

        WorldCastSpellCommand? command = null;

        if (IsCastAllowed(player, WorldAiSpellEffectType.Weaken, GetWeakenCastLimit(state.Difficulty)))
        {
            command = FindCast(state, weakenSpells, [target]);
        }

        WorldPlayer targetOwner = state.Players[target.OwnerId];

        if (command is null &&
            WorldHelpers.IsRacePlayable(targetOwner.Race) &&
            !power.AtLeast(BattleSide.Attacker, engine.Globals.AiSummonMaxPowerPercent) &&
            IsCastAllowed(player, WorldAiSpellEffectType.Summon, engine.Globals.AiSummonCastLimit))
        {
            command = FindSummonCast(state, summonSpells, stack, target);
        }

        return command;
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

    private WorldCastSpellCommand? FindSummonCast(WorldState state, List<WorldSpellDefinition> spells, WorldStack stack, WorldStack target)
    {
        foreach (WorldSpellDefinition spell in spells)
        {
            WorldCastSpellCommand? best = null;
            int bestDistance = 0;

            foreach (WorldPosition position in target.Position.GetAdjacentPositions())
            {
                int distance = position.GetDistance(stack.Position);

                if (distance > bestDistance)
                {
                    WorldCastSpellCommand command = new(spell.Id, new WorldSpellTarget(position, 0));
                    WorldCommandResult commandResult = command.Execute(state, engine);

                    if (!commandResult.Failed)
                    {
                        best = command;
                        bestDistance = distance;
                    }
                }
            }

            if (best is not null)
            {
                return best;
            }
        }

        return null;
    }

    private bool IsCastAllowed(WorldPlayer player, WorldAiSpellEffectType type, int castLimit)
    {
        int casts = 0;

        foreach ((WorldSpellDefinitionId spellId, int count) in player.TurnActions.SpellCasts)
        {
            WorldSpellDefinition spell = engine.Definitions.GetSpell(spellId);

            if (spell.AreaRadius is 0 && GetEffectType(spell) == type)
            {
                casts += count;
            }
        }

        return casts < castLimit;
    }

    private int GetWeakenCastLimit(WorldDifficulty difficulty)
    {
        return difficulty switch
        {
            WorldDifficulty.Easy => engine.Globals.AiWeakenCastLimitEasy,
            WorldDifficulty.Average => engine.Globals.AiWeakenCastLimitAverage,
            WorldDifficulty.Hard => engine.Globals.AiWeakenCastLimitHard,
            WorldDifficulty.VeryHard => engine.Globals.AiWeakenCastLimitVeryHard
        };
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
