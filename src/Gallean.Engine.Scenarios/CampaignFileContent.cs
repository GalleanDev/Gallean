namespace Gallean.Engine.Scenarios;

public sealed record CampaignFileContent(CampaignFileHeader Header, IReadOnlyList<CampaignFileEntry> Scenarios);
