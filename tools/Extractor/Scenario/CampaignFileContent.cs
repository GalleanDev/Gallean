namespace Extractor;

internal sealed record CampaignFileContent(CampaignFileHeader Header, IReadOnlyList<CampaignFileEntry> Scenarios);
