using Gallean.Common;

namespace Gallean.Engine;

internal sealed class HealSpellResolver : WorldAbstractUnitSpellResolver<WorldHealSpellEffect>, IStaticInstance<HealSpellResolver>
{
    protected override bool IsTarget(
        WorldState state,
        IGameDefinitionProvider definitions,
        WorldPlayerId casterId,
        WorldStack stack,
        WorldSpellDefinition spell)
    {
        return stack.OwnerId == casterId;
    }

    protected override WorldCommandResult OnHit(
        IEngineContext context,
        WorldPlayerId casterId,
        WorldStack stack,
        int powerPercent,
        WorldHealSpellEffect effect)
    {
        List<WorldEvent> events = [];
        int hitpoints = effect.Hitpoints * powerPercent / 100;

        foreach ((WorldUnit unit, GridPosition _) in stack.Units.GetUnits())
        {
            UnitStats stats = context.UnitStats.GetWorldUnitStats(unit);

            if (unit.IsAlive && unit.Hitpoints < stats.MaxHitpoints)
            {
                events.Add(new WorldUnitHealedEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    unit.Id,
                    Math.Min(unit.Hitpoints + hitpoints, stats.MaxHitpoints),
                    WorldResourceSet.Empty));
            }
        }

        return WorldCommandResult.Success(Empty.Value, events);
    }
}
