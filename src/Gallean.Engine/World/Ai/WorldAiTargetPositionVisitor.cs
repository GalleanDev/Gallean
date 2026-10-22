using Gallean.Common;

namespace Gallean.Engine;

public sealed class WorldAiTargetPositionVisitor : IWorldAiTargetVisitor<WorldState, WorldPosition?>, IStaticInstance<WorldAiTargetPositionVisitor>
{
    public WorldPosition? Visit(WorldAiTarget.Stack target, WorldState context)
    {
        if (context.Stacks.TryGetValue(target.StackId, out WorldStack? stack))
        {
            return stack.Position;
        }

        return null;
    }

    public WorldPosition? Visit(WorldAiTarget.Fortification target, WorldState context)
    {
        if (context.Fortifications.TryGetValue(target.FortificationId, out WorldFortification? fortification))
        {
            return fortification.Entrance;
        }

        return null;
    }

    public WorldPosition? Visit(WorldAiTarget.Location target, WorldState context)
    {
        return target.Position;
    }

    public WorldPosition? Visit(WorldAiTarget.Bag target, WorldState context)
    {
        if (context.Bags.TryGetValue(target.BagId, out WorldBag? bag))
        {
            return bag.Position;
        }

        return null;
    }
}
