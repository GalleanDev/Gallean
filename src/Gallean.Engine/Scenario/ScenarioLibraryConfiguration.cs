using System.Collections.Immutable;

namespace Gallean.Engine;

public sealed record ScenarioLibraryConfiguration(
    string GlobalDatabasePath,
    string ScenarioDatabasePath,
    string CampaignDatabasePath,
    ImmutableDictionary<Language, ScenarioLibraryLanguageConfiguration> Languages);
