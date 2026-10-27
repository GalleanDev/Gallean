namespace Gallean.Engine;

internal static class WorldBattleModifierCalculator
{
    public static BattleSideModifiers GetModifiers(WorldState state, WorldPlayerId? ownerId, bool canRetreat, IEngineContext context)
    {
        return new BattleSideModifiers(
            GetExperiencePercent(state, ownerId, context),
            GetAccuracyBonus(state, ownerId, context),
            GetRandomDamageBonus(state, ownerId, context),
            canRetreat);
    }

    private static int GetExperiencePercent(WorldState state, WorldPlayerId? ownerId, IEngineContext context)
    {
        if (ownerId is null || state.Players[ownerId.Value].Race == WorldRace.Neutral)
        {
            return 0;
        }

        if (IsAiControlled(state, ownerId))
        {
            if (context.Scenario.Definition.SuggestedLevel < context.Globals.ExperienceBonusAiMinSuggestedLevel)
            {
                return 0;
            }
            else
            {
                return state.Difficulty switch
                {
                    WorldDifficulty.Easy => context.Globals.ExperienceBonusAiEasy,
                    WorldDifficulty.Average => context.Globals.ExperienceBonusAiAverage,
                    WorldDifficulty.Hard => context.Globals.ExperienceBonusAiHard,
                    WorldDifficulty.VeryHard => context.Globals.ExperienceBonusAiVeryHard
                };
            }
        }
        else
        {
            return state.Difficulty switch
            {
                WorldDifficulty.Easy => context.Globals.ExperienceBonusHumanEasy,
                WorldDifficulty.Average => context.Globals.ExperienceBonusHumanAverage,
                WorldDifficulty.Hard => context.Globals.ExperienceBonusHumanHard,
                WorldDifficulty.VeryHard => context.Globals.ExperienceBonusHumanVeryHard
            };
        }
    }

    private static int GetAccuracyBonus(WorldState state, WorldPlayerId? ownerId, IEngineContext context)
    {
        if (IsAiControlled(state, ownerId))
        {
            return state.Difficulty switch
            {
                WorldDifficulty.Easy => context.Globals.AccuracyBonusAiEasy,
                WorldDifficulty.Average => context.Globals.AccuracyBonusAiAverage,
                WorldDifficulty.Hard => context.Globals.AccuracyBonusAiHard,
                WorldDifficulty.VeryHard => context.Globals.AccuracyBonusAiVeryHard
            };
        }
        else
        {
            return 0;
        }
    }

    private static int GetRandomDamageBonus(WorldState state, WorldPlayerId? ownerId, IEngineContext context)
    {
        if (!IsAiControlled(state, ownerId) && state.Difficulty is WorldDifficulty.Easy)
        {
            return context.Globals.BattleDamageVariance;
        }
        else
        {
            return 0;
        }
    }

    private static bool IsAiControlled(WorldState state, WorldPlayerId? ownerId)
    {
        if (ownerId is null)
        {
            return true;
        }
        else
        {
            return state.Players[ownerId.Value].IsAi;
        }
    }
}
