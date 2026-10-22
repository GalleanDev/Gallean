namespace Gallean.Configuration;

public static class AppDirectories
{
    private static string LocalApplicationData { get; } = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    public static string Root { get; } = Path.Combine(LocalApplicationData, "Gallean");
    public static string Assets { get; } = Path.Combine(Root, "Assets");
    public static string Downloads { get; } = Path.Combine(Root, "Downloads");
    public static string EnhanceCache { get; } = Path.Combine(Root, "Enhance", "Cache");
    public static string EnhanceModels { get; } = Path.Combine(Root, "Enhance", "Models");
    public static string Libraries { get; } = Path.Combine(Root, "Libraries");
    public static string Logs { get; } = Path.Combine(Root, "Logs");
    public static string OriginalAssets { get; } = Path.Combine(Root, "OriginalAssets");
    public static string Saves { get; } = Path.Combine(Root, "Saves");
}
