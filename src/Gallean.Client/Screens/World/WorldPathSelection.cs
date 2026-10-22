using Gallean.Engine;

namespace Gallean.Client;

internal sealed record WorldPathSelection(
    WorldPosition Destination,
    WorldPath Path,
    WorldPath Remainder,
    WorldPathEncounter? Encounter);
