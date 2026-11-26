namespace Gallean.Engine.Scenarios;

public sealed record CampaignFile(CampaignFileHeader Header, IReadOnlyList<CampaignFileScenario> Scenarios);
