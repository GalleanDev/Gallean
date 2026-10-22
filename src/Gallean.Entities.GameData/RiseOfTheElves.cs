namespace Gallean.Entities.GameData;

public static class RiseOfTheElves
{
    private const string RootDirectoryName = "RiseOfTheElves";
    private const string GlobalsDirectoryName = "Globals";
    private const string ScenariosDirectoryName = "Scens";
    private const string CampaignsDirectoryName = "Campaign";

    public static string RootPath => Path.Combine(AppContext.BaseDirectory, RootDirectoryName);
    public static string GlobalDatabasePath => Path.Combine(RootPath, GlobalsDirectoryName);
    public static string ScenarioDatabasePath => Path.Combine(RootPath, ScenariosDirectoryName);
    public static string CampaignDatabasePath => Path.Combine(RootPath, CampaignsDirectoryName);
}
