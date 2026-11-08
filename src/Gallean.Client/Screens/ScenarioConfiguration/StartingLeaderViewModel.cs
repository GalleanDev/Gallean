using Gallean.Configuration;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed record StartingLeaderViewModel(
    UnitDefinitionId LeaderId,
    UnitLeaderCategory Category,
    string Name,
    string Description,
    LeaderEntry? Entry);
