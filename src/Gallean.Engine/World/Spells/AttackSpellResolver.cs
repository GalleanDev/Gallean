using Gallean.Common;

namespace Gallean.Engine;

internal sealed class AttackSpellResolver : WorldAbstractUnitSpellResolver<WorldAttackSpellEffect>, IStaticInstance<AttackSpellResolver>
{
    protected override bool IsTarget(
        WorldState state,
        IGameDefinitionProvider definitions,
        WorldPlayerId casterId,
        WorldStack stack,
        WorldSpellDefinition spell)
    {
        return DiplomacyCalculator.IsHostile(state, casterId, stack.OwnerId);
    }

    protected override WorldCommandResult OnHit(
        IEngineContext context,
        WorldPlayerId? casterId,
        WorldStack stack,
        int powerPercent,
        WorldAttackSpellEffect effect)
    {
        List<WorldEvent> events = [];
        int damage = effect.Damage * powerPercent / 100;
        (_, _, UnitDefinition leaderDefinition) = stack.GetLeader(context.Definitions);

        if (leaderDefinition.Category is not UnitCategory.Illusion)
        {
            foreach ((WorldUnit unit, GridPosition _) in stack.Units.GetUnits())
            {
                if (unit.IsAlive && !IsUnitImmune(unit, context.Definitions, effect))
                {
                    events.Add(new WorldUnitDamagedEvent(
                        new WorldUnitGroup.Stack(stack.Id),
                        unit.Id,
                        Math.Max(0, unit.Hitpoints - damage),
                        casterId));
                }
            }
        }

        if (events.Count > 0)
        {
            return WorldCommandResult.Success(Empty.Value, events);
        }
        else
        {
            return WorldCommandResult.Fail(WorldCommandError.SpellTargetImmune);
        }
    }

    private static bool IsUnitImmune(WorldUnit unit, IGameDefinitionProvider definitions, WorldAttackSpellEffect effect)
    {
        foreach (UnitWard unitWard in WorldUnitWardCalculator.GetWards(unit, definitions))
        {
            if (unitWard is UnitAttackSourceWard { Category: UnitWardCategory.Always } ward &&
                ward.Source == effect.Source)
            {
                return true;
            }
        }

        return false;
    }
}
