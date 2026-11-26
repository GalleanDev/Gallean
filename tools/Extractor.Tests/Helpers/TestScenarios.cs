namespace Extractor.Tests;

internal static class TestScenarios
{
    private const string DirectoryName = "TestScenarios";

    private static string DirectoryPath => Path.Combine(AppContext.BaseDirectory, DirectoryName);

    public static ScenarioTextEncoding Encoding { get; } = ScenarioTextEncoding.FromCodePage(ScenarioTextEncoding.DefaultCodePage);

    public static string GreywaterCrossingPath => Path.Combine(DirectoryPath, "GreywaterCrossing.sg");
    public static string GreywaterSiegePath => Path.Combine(DirectoryPath, "GreywaterSiege.sg");
}
