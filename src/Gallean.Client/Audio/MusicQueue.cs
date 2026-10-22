using System.Collections.Immutable;
using System.Diagnostics;
using Gallean.Common;

namespace Gallean.Client;

internal sealed partial record MusicQueue
{
    private MusicQueue(ImmutableArray<AssetSoundKey> tracks, bool isLooping)
    {
        Tracks = tracks;
        IsLooping = isLooping;
    }

    public ImmutableArray<AssetSoundKey> Tracks { get; }
    public bool IsLooping { get; }

    public bool Equals(MusicQueue? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return IsLooping == other.IsLooping &&
               Tracks.SequenceEqual(other.Tracks);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(IsLooping);
        hash.AddSequence(Tracks);

        return hash.ToHashCode();
    }

    public static MusicQueue Create(IEnumerable<AssetSoundKey> tracks, bool isLooping)
    {
        ImmutableArray<AssetSoundKey> ordered = [.. tracks];

        Debug.Assert(ordered.Length > 0);

        return new MusicQueue(ordered, isLooping);
    }
}
