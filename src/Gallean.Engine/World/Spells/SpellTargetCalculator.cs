namespace Gallean.Engine;

internal static class SpellTargetCalculator
{
    public static bool IsTargetValid(
        WorldState state,
        WorldPlayer player,
        WorldSpellDefinition spell,
        WorldSpellTarget? target)
    {
        if (spell.AreaRadius is null)
        {
            return target is null;
        }
        else
        {
            return target is not null &&
                   state.Map.IsValidPosition(target.Center) &&
                   player.FogOfWar.Get(target.Center.X, target.Center.Y);
        }
    }

    public static bool IsTargetConcealed(WorldState state, WorldPlayerId casterId, WorldSpellTarget? target)
    {
        if (target is null)
        {
            return false;
        }

        WorldPlan plan = WorldPlan.For(state);
        WorldStack? stack = plan.GetStackAt(target.Center);

        return stack is not null && stack.Concealed && stack.OwnerId != casterId;
    }

    public static WorldPosition? FindSummonTarget(WorldState state, WorldSpellTarget target, UnitDefinition unit)
    {
        WorldPlan plan = WorldPlan.For(state);
        WorldMovementCalculator.MovementProfile profile = new(unit.MoveAbility, unit.IsWaterOnly, IsLeaderDead: false, IsThief: false);

        foreach (WorldPosition position in EnumerateTarget(state, target))
        {
            if (plan.CanPlaceStackAt(position, profile))
            {
                return position;
            }
        }

        return null;
    }

    public static WorldPosition? FindStackTarget(WorldState state, WorldSpellTarget target)
    {
        WorldPlan plan = WorldPlan.For(state);

        foreach (WorldPosition position in EnumerateTarget(state, target))
        {
            if (plan.GetStackAt(position) is not null)
            {
                return position;
            }
        }

        return null;
    }

    public static IEnumerable<WorldPosition> GetArea(WorldState state, WorldPosition? position, int? areaRadius)
    {
        WorldMap map = state.Map;

        if (areaRadius is not null && position is not null)
        {
            return EnumerateBox(map, position.Value, areaRadius.Value);
        }
        else
        {
            return EnumerateBox(map, new WorldPosition(0, 0), Math.Max(map.Width, map.Height));
        }
    }

    public static int GetPowerPercent(WorldPosition? center, WorldPosition position, GlobalVariables globals)
    {
        if (center is not null)
        {
            int ring = Math.Max(Math.Abs(position.X - center.Value.X), Math.Abs(position.Y - center.Value.Y));

            return ring switch
            {
                0 => globals.SpellPowerRing0,
                1 => globals.SpellPowerRing1,
                2 => globals.SpellPowerRing2,
                3 => globals.SpellPowerRing3,
                4 => globals.SpellPowerRing4,
                _ => 0
            };
        }
        else
        {
            return globals.SpellPowerRing0;
        }
    }

    private static IEnumerable<WorldPosition> EnumerateTarget(WorldState state, WorldSpellTarget target)
    {
        yield return target.Center;

        foreach (WorldPosition position in EnumerateBox(state.Map, target.Center, target.Radius))
        {
            if (position != target.Center)
            {
                yield return position;
            }
        }
    }

    private static IEnumerable<WorldPosition> EnumerateBox(WorldMap map, WorldPosition center, int radius)
    {
        int minY = Math.Max(0, center.Y - radius);
        int maxY = Math.Min(map.Height - 1, center.Y + radius);
        int minX = Math.Max(0, center.X - radius);
        int maxX = Math.Min(map.Width - 1, center.X + radius);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                yield return new WorldPosition(x, y);
            }
        }
    }
}
