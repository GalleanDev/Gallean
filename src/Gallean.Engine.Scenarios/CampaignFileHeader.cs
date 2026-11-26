namespace Gallean.Engine.Scenarios;

public sealed record CampaignFileHeader(string Name, string Author, Db.Id PlayRaceId, bool HasSavedGame);
