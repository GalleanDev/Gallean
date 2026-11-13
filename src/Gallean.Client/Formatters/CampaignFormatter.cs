namespace Gallean.Client;

internal static class CampaignFormatter
{
    public static string FormatName(GameCampaign campaign)
    {
        return campaign.Id switch
        {
            "C000CC0002" or "C000CC0003" or "C000CC0004" or "C000CC0005" => Tr("Dark Prophecy"),
            "C000CC0006" => Tr("Rise of the Elves"),
            "C000CC0012" or "C000CC0013" => Tr("Guardians of the Light"),
            "C000CC0014" or "C000CC0015" => Tr("Servants of the Dark"),
            _ => campaign.Name
        };
    }
}
