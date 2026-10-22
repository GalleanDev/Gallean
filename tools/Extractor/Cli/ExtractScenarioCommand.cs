using System.CommandLine;
using System.CommandLine.Parsing;

namespace Extractor;

internal class ExtractScenarioCommand : Command
{
    private const string ScenariosDirectoryName = "Scens";
    private const string CampaignDirectoryName = "Campaign";

    public ExtractScenarioCommand(Option<string> outputOption)
        : base("extract-scenario", "Extracts scenario tables from binary scenario (.sg) and campaign (.csg) files.")
    {
        Option<string> inputOption = new("--input", "-i")
        {
            Description = "Path to a folder with .sg and .csg files",
            Required = true,
            Validators =
            {
                option =>
                {
                    string input = option.GetValueOrDefault<string>();

                    if (!Directory.Exists(input))
                    {
                        option.AddError($"The directory '{input}' does not exist.");
                    }
                }
            }
        };

        Option<int> codePageOption = new("--code-page")
        {
            Description = "Code page",
            DefaultValueFactory = _ => ScenarioTextEncoding.DefaultCodePage,
            Validators =
            {
                option =>
                {
                    foreach (Token token in option.Tokens)
                    {
                        if (int.TryParse(token.Value, out int codePage))
                        {
                            if (ScenarioTextEncoding.TryFromCodePage(codePage) is null)
                            {
                                option.AddError($"Unknown code page '{token.Value}'.");
                            }
                        }
                    }
                }
            }
        };

        Options.Add(inputOption);
        Options.Add(codePageOption);

        SetAction(parseResult =>
        {
            string input = parseResult.GetRequiredValue(inputOption);
            string output = parseResult.GetRequiredValue(outputOption);
            int codePage = parseResult.GetRequiredValue(codePageOption);
            ScenarioTextEncoding encoding = ScenarioTextEncoding.FromCodePage(codePage);
            Execute(input, output, encoding);
        });
    }

    internal static IReadOnlyList<ScenarioLibraryEntry> Execute(
        string input,
        string output,
        ScenarioTextEncoding encoding)
    {
        string scenarioDirectory = Path.Combine(output, ScenariosDirectoryName);
        string campaignDirectory = Path.Combine(output, CampaignDirectoryName);

        Directory.CreateDirectory(scenarioDirectory);
        Directory.CreateDirectory(campaignDirectory);

        Console.WriteLine($"Extracting scenarios from '{input}'...");

        ScenarioLibraryBuilder builder = new(scenarioDirectory, campaignDirectory, encoding);
        IReadOnlyList<ScenarioLibraryEntry> entries = builder.Add(input);

        foreach (ScenarioLibraryEntry entry in entries)
        {
            Console.WriteLine($"  {entry.ScenarioId} <- {entry.Source} ({entry.Name})");
        }

        Console.WriteLine($"Scenario extraction completed: {entries.Count} scenarios -> {scenarioDirectory}");

        return entries;
    }
}
