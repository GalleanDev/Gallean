namespace Gallean.Engine;

internal static class WorldStateExtensions
{
    extension(WorldState state)
    {
        public WorldPlayer CurrentPlayer => state.Players[state.TurnInfo.CurrentPlayerId];

        public WorldPlayerId GetFirstPlayerId()
        {
            List<WorldPlayer> players = GetTurnOrder(state);

            return players[0].Id;
        }

        public WorldPlayerId GetNextPlayerId(WorldPlayerId playerId)
        {
            List<WorldPlayer> players = GetTurnOrder(state);

            int index = players.FindIndex(player => player.Id == playerId);

            return players[(index + 1) % players.Count].Id;
        }
    }

    private static List<WorldPlayer> GetTurnOrder(WorldState state)
    {
        List<WorldPlayer> players = [.. state.Players.Values];

        players.Sort(ComparePlayerTurnOrder);

        return players;
    }

    private static int ComparePlayerTurnOrder(WorldPlayer left, WorldPlayer right)
    {
        int rv;

        rv = GetOrder(left).CompareTo(GetOrder(right));
        if (rv != 0)
        {
            return rv;
        }

        rv = left.Id.CompareTo(right.Id);
        if (rv != 0)
        {
            return rv;
        }

        return 0;

        static int GetOrder(WorldPlayer player)
        {
            if (player.IsAi)
            {
                return player.Race switch
                {
                    WorldRace.Human => 1,
                    WorldRace.Heretic => 2,
                    WorldRace.Dwarf => 3,
                    WorldRace.Undead => 4,
                    WorldRace.Elf => 5,
                    WorldRace.Neutral => 6
                };
            }
            else
            {
                return 0;
            }
        }
    }
}
