using Gallean.Common;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed record WorldAmbientSoundState(IReadOnlyList<WorldAmbientSoundState.Sound> Sounds)
{
    public sealed record Sound(AmbientSound Source, float Volume, float Pan);

    public bool Equals(WorldAmbientSoundState? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Sounds.SequenceEqual(other.Sounds);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.AddSequence(Sounds);

        return hash.ToHashCode();
    }
}
