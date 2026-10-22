namespace Gallean.Engine;

internal sealed class StealSpellThiefActionResolver(WorldThiefStealSpellAction action) : AbstractThiefActionResolver(action.TargetPosition)
{
    protected override AttemptResult AttemptAction(
        WorldState state,
        IEngineContext context,
        WorldStack thief,
        WorldPlayerId player,
        ThiefTarget target)
    {
        if (target is ThiefFortificationTarget fortificationTarget)
        {
            WorldPlayerId ownerId = fortificationTarget.Owner;

            if (!state.Players[ownerId].Spellbook.Acquired.Contains(action.SpellId))
            {
                return AttemptResult.Fail(WorldCommandError.SpellNotKnown);
            }

            WorldSpellDefinition spell = context.Definitions.GetSpell(action.SpellId);

            if (spell.Effect is WorldSummonStackSpellEffect)
            {
                return AttemptResult.Fail(WorldCommandError.SpellNotAvailable);
            }

            if (state.Players[player].Spellbook.Acquired.Contains(action.SpellId))
            {
                return AttemptResult.Fail(WorldCommandError.SpellAlreadyKnown);
            }

            return AttemptResult.Success(
                WorldThiefActionType.StealSpell,
                ownerId,
                [new WorldSpellStolenEvent(thief.Id, ownerId, action.SpellId)]);
        }
        else if (target is ThiefMageSiteTarget mageTarget)
        {
            WorldMageSite mageSite = mageTarget.Site;

            if (!mageSite.Spells.Contains(action.SpellId))
            {
                return AttemptResult.Fail(WorldCommandError.SpellNotAvailable);
            }

            if (state.Players[player].Spellbook.Acquired.Contains(action.SpellId))
            {
                return AttemptResult.Fail(WorldCommandError.SpellAlreadyKnown);
            }

            return AttemptResult.Success(
                WorldThiefActionType.StealMage,
                mageTarget.OwnerId,
                [new WorldSpellStolenEvent(thief.Id, TargetPlayerId: null, action.SpellId)]);
        }
        else
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }
    }
}
