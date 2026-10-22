using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    public sealed record PlayerRaces
    {
        private readonly ImmutableSortedDictionary<WorldPlayerId, WorldRace> races;

        private PlayerRaces(ImmutableSortedDictionary<WorldPlayerId, WorldRace> races)
        {
            this.races = races;
        }

        public WorldRace this[WorldPlayerId playerId] => races[playerId];

        public bool Equals(PlayerRaces? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return races.SequenceEqual(other.races);
        }

        public static PlayerRaces Create(ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> players)
        {
            ImmutableSortedDictionary<WorldPlayerId, WorldRace>.Builder builder =
                ImmutableSortedDictionary.CreateBuilder<WorldPlayerId, WorldRace>();

            foreach (WorldPlayer player in players.Values)
            {
                builder.Add(player.Id, player.Race);
            }

            return new PlayerRaces(builder.ToImmutable());
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.AddSequence(races);

            return hash.ToHashCode();
        }
    }
}
