namespace Extractor;

internal sealed record ScenarioFile(Db.ScenarioId ScenarioId, ScenarioFileFormat Format, ScenarioFileTables Tables);
