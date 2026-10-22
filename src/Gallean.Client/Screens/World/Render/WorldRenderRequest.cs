using System.Runtime.CompilerServices;

namespace Gallean.Client;

internal sealed record WorldRenderRequest(
    WorldState State,
    WorldPlayerId ViewPlayerId,
    WorldCursor Cursor,
    bool FogEnabled,
    WorldAnimationState? Animation)
{
    public bool Equals(WorldRenderRequest? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return ReferenceEquals(State, other.State) &&
               ViewPlayerId == other.ViewPlayerId &&
               Cursor == other.Cursor &&
               FogEnabled == other.FogEnabled &&
               Animation == other.Animation;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(RuntimeHelpers.GetHashCode(State));
        hash.Add(ViewPlayerId);
        hash.Add(Cursor);
        hash.Add(FogEnabled);
        hash.Add(Animation);

        return hash.ToHashCode();
    }
}
