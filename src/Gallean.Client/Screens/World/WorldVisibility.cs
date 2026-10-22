using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal readonly record struct WorldVisibility(int Width, int Height, ImmutableBitmap? Fog)
{
    public static WorldVisibility Create(WorldState state, WorldPlayerId viewPlayerId, bool fogEnabled)
    {
        if (fogEnabled)
        {
            return new WorldVisibility(state.Map.Width, state.Map.Height, state.Players[viewPlayerId].FogOfWar);
        }
        else
        {
            return new WorldVisibility(state.Map.Width, state.Map.Height, null);
        }
    }

    public WorldStack? GetStackAt(WorldPlan plan, WorldPosition position, WorldPlayerId viewPlayerId)
    {
        WorldStack? stack = plan.GetStackAt(position);

        if (stack is not null && IsStackVisible(stack, viewPlayerId))
        {
            return stack;
        }
        else
        {
            return null;
        }
    }

    public WorldFortification? GetFortificationAt(WorldPlan plan, WorldPosition position)
    {
        if (IsRevealed(position))
        {
            return plan.GetFortificationAt(position);
        }
        else
        {
            return null;
        }
    }

    public WorldSite? GetSiteAt(WorldPlan plan, WorldPosition position)
    {
        if (IsRevealed(position))
        {
            return plan.GetSiteAt(position);
        }
        else
        {
            return null;
        }
    }

    public WorldBag? GetBagAt(WorldPlan plan, WorldPosition position)
    {
        if (IsRevealed(position))
        {
            return plan.GetBagAt(position);
        }
        else
        {
            return null;
        }
    }

    public bool IsStackVisible(WorldStack stack, WorldPlayerId viewPlayerId)
    {
        if (IsRevealed(stack.Position))
        {
            if (stack.Concealed)
            {
                return stack.OwnerId == viewPlayerId;
            }
            else
            {
                return true;
            }
        }
        else
        {
            return false;
        }
    }

    public bool IsExplored(WorldPosition position)
    {
        if (Fog is not null && IsValidPosition(position))
        {
            return Fog.Get(position.X, position.Y);
        }
        else
        {
            return false;
        }
    }

    public bool IsRevealed(WorldPosition position)
    {
        return Fog is null || IsExplored(position);
    }

    public bool IsAnyRevealed(WorldFootprint footprint)
    {
        return IsAnyRevealed(footprint.EnumerateTiles());
    }

    public bool IsAnyRevealed(IEnumerable<WorldPosition> positions)
    {
        foreach (WorldPosition position in positions)
        {
            if (IsRevealed(position))
            {
                return true;
            }
        }

        return false;
    }

    public static bool KnowsPlayerContents(WorldState state, WorldPlayerId viewPlayerId, WorldPlayerId ownerId)
    {
        if (viewPlayerId == ownerId || state.Players[ownerId].Race == WorldRace.Neutral)
        {
            return true;
        }
        else
        {
            return state.Players[viewPlayerId].SpiedPlayers.Contains(ownerId);
        }
    }

    public static bool KnowsRuinContents(WorldState state, WorldPlayerId viewPlayerId, WorldFortificationId ruinId)
    {
        return state.Players[viewPlayerId].SpiedRuins.Contains(ruinId);
    }

    private bool IsValidPosition(WorldPosition position)
    {
        return position.X >= 0 && position.X < Width && position.Y >= 0 && position.Y < Height;
    }
}
