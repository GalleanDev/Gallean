namespace Extractor;

internal sealed record ScenarioFileFormat(int? SaveVersion)
{
    public const string ScenarioSignature = "D2EESFISIG";
    public const string SaveSignature = "MidFile";

    public static ScenarioFileFormat Scenario { get; } = new(SaveVersion: null);

    public bool HasDesignData => SaveVersion is null or 0;

    public static ScenarioFileFormat Save(int version)
    {
        return new ScenarioFileFormat(version);
    }
}
