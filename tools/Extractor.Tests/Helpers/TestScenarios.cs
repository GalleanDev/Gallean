namespace Extractor.Tests;

internal static class TestScenarios
{
    private const string DirectoryName = "TestScenarios";

    private static string DirectoryPath => Path.Combine(AppContext.BaseDirectory, DirectoryName);

    public static ScenarioTextEncoding Encoding { get; } = ScenarioTextEncoding.FromCodePage(ScenarioTextEncoding.DefaultCodePage);

    public static string DoragonRealmPath => Path.Combine(DirectoryPath, "DoragonRealm.sg");
    public static string TutorialPath => Path.Combine(DirectoryPath, "Tutorial.sg");
}
