using Gallean.Configuration;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed record GameConfiguration(string ScenarioId, ScenarioConfiguration? Configuration, CustomScenarioId? CustomScenarioId);
