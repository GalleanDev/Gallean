using Gallean.Common;

namespace Gallean.Engine;

internal sealed class InvisibilitySpellResolver : WorldAbstractUnitSpellResolver<WorldInvisibilitySpellEffect>, IStaticInstance<InvisibilitySpellResolver>
{
    protected override bool IsTarget(
        WorldState state,
        IGameDefinitionProvider definitions,
        WorldPlayerId casterId,
        WorldStack stack,
        WorldSpellDefinition spell)
    {
        if (stack.OwnerId != casterId)
        {
            return false;
        }

        if (spell.AreaRadius is not null)
        {
            (_, _, UnitDefinition leaderDefinition) = stack.GetLeader(definitions);

            return leaderDefinition.Category is not UnitCategory.Illusion;
        }
        else
        {
            return true;
        }
    }

    protected override WorldCommandResult OnHit(
        IEngineContext context,
        WorldPlayerId? casterId,
        WorldStack stack,
        int powerPercent,
        WorldInvisibilitySpellEffect effect)
    {
        List<WorldEvent> events = [];

        if (!stack.Concealed)
        {
            events.Add(new WorldStackConcealedEvent(stack.Id));
        }

        return WorldCommandResult.Success(Empty.Value, events);
    }
}
