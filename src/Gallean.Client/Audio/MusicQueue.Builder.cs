using System.Collections.Immutable;
using System.Diagnostics;
using Gallean.Common;

namespace Gallean.Client;

internal sealed partial record MusicQueue
{
    public sealed class Builder(IRandomProvider random)
    {
        private readonly List<ImmutableArray<AssetSoundKey>> playlists = [];

        public Builder AddShuffled(IEnumerable<AssetSoundKey> tracks)
        {
            AssetSoundKey[] shuffled = [.. tracks];

            random.Shuffle(shuffled);
            playlists.Add([.. shuffled]);

            return this;
        }

        public MusicQueue Build()
        {
            ImmutableArray<AssetSoundKey>[] order = [.. playlists];

            random.Shuffle(order);

            int longest = 0;

            foreach (ImmutableArray<AssetSoundKey> playlist in order)
            {
                longest = Math.Max(longest, playlist.Length);
            }

            ImmutableArray<AssetSoundKey>.Builder tracks = ImmutableArray.CreateBuilder<AssetSoundKey>();

            for (int index = 0; index < longest; index++)
            {
                foreach (ImmutableArray<AssetSoundKey> playlist in order)
                {
                    if (index < playlist.Length)
                    {
                        tracks.Add(playlist[index]);
                    }
                }
            }

            Debug.Assert(tracks.Count > 0);

            return new MusicQueue(tracks.ToImmutable(), isLooping: true);
        }
    }
}
