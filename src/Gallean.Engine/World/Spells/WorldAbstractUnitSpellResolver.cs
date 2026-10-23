using Gallean.Common;

namespace Gallean.Engine;

internal abstract class WorldAbstractUnitSpellResolver<TEffect> : IWorldSpellResolver<TEffect>
    where TEffect : WorldAbstractSpellEffect
{
    public WorldCommandResult Resolve(
        WorldState state,
        IEngineContext context,
        WorldPlayerId? casterId,
        WorldSpellTarget? target,
        WorldSpellDefinition spell,
        TEffect effect)
    {
        WorldPosition? position = null;

        if (target is not null)
        {
            position = SpellTargetCalculator.FindStackTarget(state, target) ?? target.Center;
        }

        WorldPlan plan = WorldPlan.For(state);

        if (position is not null && plan.GetFortificationAt(position.Value) is not null)
        {
            return WorldCommandResult.Fail(WorldCommandError.SpellTargetInCity);
        }

        List<(WorldStack Stack, int PowerPercent)> footprint = [];
        List<(WorldStack Stack, int PowerPercent)> targets = [];
        bool targetInCity = false;

        foreach (WorldStack stack in EnumerateStacks(plan, state, position, spell.AreaRadius))
        {
            if (spell.AreaRadius is not null && plan.GetFortificationAt(stack.Position) is not null)
            {
                targetInCity = true;
            }
            else
            {
                int powerPercent = SpellTargetCalculator.GetPowerPercent(position, stack.Position, context.Globals);

                footprint.Add((stack, powerPercent));

                if (casterId is null || IsTarget(state, context.Definitions, casterId.Value, stack, spell))
                {
                    targets.Add((stack, powerPercent));
                }
            }
        }

        if (footprint.Count <= 0 && targetInCity)
        {
            return WorldCommandResult.Fail(WorldCommandError.SpellTargetInCity);
        }

        WorldCommandResult hitResult = HitStacks(context, casterId, targets, effect);

        if (hitResult.Failed)
        {
            return hitResult;
        }

        if (spell.AreaRadius is not null)
        {
            hitResult = HitStacks(context, casterId, footprint, effect);
        }

        return hitResult;
    }

    protected abstract bool IsTarget(WorldState state, IGameDefinitionProvider definitions, WorldPlayerId casterId, WorldStack stack, WorldSpellDefinition spell);

    protected abstract WorldCommandResult OnHit(
        IEngineContext context,
        WorldPlayerId? casterId,
        WorldStack stack,
        int powerPercent,
        TEffect effect);

    private WorldCommandResult HitStacks(
        IEngineContext context,
        WorldPlayerId? casterId,
        List<(WorldStack Stack, int PowerPercent)> stacks,
        TEffect effect)
    {
        List<WorldEvent> events = [];
        List<WorldCommandResult> failures = [];

        foreach ((WorldStack stack, int powerPercent) in stacks)
        {
            WorldCommandResult hitResult = OnHit(context, casterId, stack, powerPercent, effect);

            if (hitResult.Failed)
            {
                failures.Add(hitResult);
            }
            else
            {
                events.AddRange(hitResult.Events);
            }
        }

        if (events.Count > 0)
        {
            return WorldCommandResult.Success(Empty.Value, events);
        }
        else if (failures.Count > 0)
        {
            return failures[0];
        }
        else
        {
            return WorldCommandResult.Fail(WorldCommandError.SpellTargetInvalid);
        }
    }

    private static IEnumerable<WorldStack> EnumerateStacks(
        WorldPlan plan,
        WorldState state,
        WorldPosition? position,
        int? areaRadius)
    {
        foreach (WorldPosition tile in SpellTargetCalculator.GetArea(state, position, areaRadius))
        {
            WorldStack? stack = plan.GetStackAt(tile);

            if (stack is not null)
            {
                yield return stack;
            }
        }
    }
}
