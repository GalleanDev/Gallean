using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal static class FogOfWarCalculator
{
    public static ImmutableArray<WorldPosition> Diff(ImmutableBitmap before, ImmutableBitmap after)
    {
        ImmutableArray<WorldPosition>.Builder revealed = ImmutableArray.CreateBuilder<WorldPosition>();

        for (int y = 0; y < after.Height; y++)
        {
            for (int x = 0; x < after.Width; x++)
            {
                if (after.Get(x, y) && !before.Get(x, y))
                {
                    revealed.Add(new WorldPosition(x, y));
                }
            }
        }

        return revealed.ToImmutable();
    }

    public static ImmutableBitmap Update(
        WorldState state,
        WorldPlayerId playerId,
        IEngineContext context)
    {
        ImmutableBitmap.Builder builder = state.Players[playerId].FogOfWar.ToBuilder();

        foreach (WorldStack stack in state.Stacks.Values)
        {
            if (stack.OwnerId == playerId)
            {
                StackStats stats = context.UnitStats.GetWorldStackStats(stack);
                RevealAroundPosition(builder, stack.Position, stats.VisionRadius);
            }
        }

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            if (FortificationCalculator.GetOwner(fortification) == playerId)
            {
                int visionRadius = FortificationCalculator.GetVisionRadius(fortification, context);

                foreach (WorldPosition tile in fortification.Footprint.EnumerateTiles())
                {
                    RevealAroundPosition(builder, tile, visionRadius);
                }
            }
        }

        return builder.ToImmutable();
    }

    private static void RevealAroundPosition(ImmutableBitmap.Builder builder, WorldPosition center, int radius)
    {
        int minY = Math.Max(0, center.Y - radius);
        int maxY = Math.Min(builder.Height - 1, center.Y + radius);
        int minX = Math.Max(0, center.X - radius);
        int maxX = Math.Min(builder.Width - 1, center.X + radius);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                builder.Set(x, y);
            }
        }
    }
}
