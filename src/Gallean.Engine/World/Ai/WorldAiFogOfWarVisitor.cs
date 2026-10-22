using Gallean.Common;

namespace Gallean.Engine;

internal sealed class WorldAiFogOfWarVisitor : IWorldStackOrderVisitor<WorldPlayer, ImmutableBitmap?>, IStaticInstance<WorldAiFogOfWarVisitor>
{
    public ImmutableBitmap? Visit(WorldStackOrder.Normal order, WorldPlayer context)
    {
        return GetPlayerFogOfWar(context);
    }

    public ImmutableBitmap? Visit(WorldStackOrder.Stand order, WorldPlayer context)
    {
        return null;
    }

    public ImmutableBitmap? Visit(WorldStackOrder.Guard order, WorldPlayer context)
    {
        return null;
    }

    public ImmutableBitmap? Visit(WorldStackOrder.AttackStack order, WorldPlayer context)
    {
        return null;
    }

    public ImmutableBitmap? Visit(WorldStackOrder.DefendStack order, WorldPlayer context)
    {
        return null;
    }

    public ImmutableBitmap? Visit(WorldStackOrder.SecureCity order, WorldPlayer context)
    {
        return null;
    }

    public ImmutableBitmap? Visit(WorldStackOrder.Roam order, WorldPlayer context)
    {
        return null;
    }

    public ImmutableBitmap? Visit(WorldStackOrder.Explore order, WorldPlayer context)
    {
        return GetPlayerFogOfWar(context);
    }

    public ImmutableBitmap? Visit(WorldStackOrder.MoveToLocation order, WorldPlayer context)
    {
        return null;
    }

    public ImmutableBitmap? Visit(WorldStackOrder.DefendLocation order, WorldPlayer context)
    {
        return null;
    }

    public ImmutableBitmap? Visit(WorldStackOrder.Berserk order, WorldPlayer context)
    {
        return null;
    }

    public ImmutableBitmap? Visit(WorldStackOrder.Assist order, WorldPlayer context)
    {
        return null;
    }

    public ImmutableBitmap? Visit(WorldStackOrder.Steal order, WorldPlayer context)
    {
        return null;
    }

    public ImmutableBitmap? Visit(WorldStackOrder.DefendCity order, WorldPlayer context)
    {
        return null;
    }

    public ImmutableBitmap? Visit(WorldStackOrder.Heal order, WorldPlayer context)
    {
        return GetPlayerFogOfWar(context);
    }

    public ImmutableBitmap? Visit(WorldStackOrder.Reinforce order, WorldPlayer context)
    {
        return GetPlayerFogOfWar(context);
    }

    private static ImmutableBitmap? GetPlayerFogOfWar(WorldPlayer player)
    {
        if (player.Race == WorldRace.Neutral)
        {
            return null;
        }
        else
        {
            return player.FogOfWar;
        }
    }
}
