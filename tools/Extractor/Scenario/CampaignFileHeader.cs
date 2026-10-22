namespace Extractor;

internal sealed record CampaignFileHeader(string Name, string Author, Db.Id PlayRaceId, bool HasSavedGame);
