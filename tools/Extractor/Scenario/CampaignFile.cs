namespace Extractor;

internal sealed record CampaignFile(CampaignFileHeader Header, IReadOnlyList<CampaignFileScenario> Scenarios);
