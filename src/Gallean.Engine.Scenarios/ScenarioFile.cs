namespace Gallean.Engine.Scenarios;

public sealed record ScenarioFile(Db.ScenarioId ScenarioId, ScenarioFileFormat Format, ScenarioFileTables Tables);
