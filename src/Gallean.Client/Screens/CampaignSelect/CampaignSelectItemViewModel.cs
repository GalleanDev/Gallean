namespace Gallean.Client;

internal sealed record CampaignSelectItemViewModel(
    GameCampaign Campaign,
    AssetSpriteKey.UnitPortrait PortraitKey,
    string RaceName,
    string Subtitle,
    bool IsExpansion,
    string? SectionTitle);
