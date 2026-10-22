using System.CommandLine;

namespace Extractor;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Option<string> gamePathOption = new("--game-path", "-g")
        {
            Description = "Path to the Disciples 2 game root directory",
            Required = true,
            Validators =
            {
                option =>
                {
                    string gamePath = option.GetValueOrDefault<string>();

                    if (!Directory.Exists(gamePath))
                    {
                        option.AddError($"The directory '{gamePath}' does not exist.");
                    }
                }
            }
        };

        Option<string> outputOption = new("--output", "-o")
        {
            Description = "Path to the output folder",
            Required = true,
            Recursive = true
        };

        RootCommand rootCommand = new("A tool to extract and convert Disciples 2 game resources");
        rootCommand.Options.Add(outputOption);

        ExtractDatabaseCommand extractDatabaseCommand = new(gamePathOption, outputOption);
        ExtractImagesCommand extractImagesCommand = new(gamePathOption, outputOption);
        ExtractInterfaceCommand extractInterfaceCommand = new(gamePathOption, outputOption);
        ExtractSoundsCommand extractSoundsCommand = new(gamePathOption, outputOption);
        ExtractAllCommand extractAllCommand = new(gamePathOption, outputOption);
        ListNamesCommand listNamesCommand = new(gamePathOption, outputOption);
        ExtractScenarioCommand extractScenarioCommand = new(outputOption);

        rootCommand.Subcommands.Add(extractDatabaseCommand);
        rootCommand.Subcommands.Add(extractImagesCommand);
        rootCommand.Subcommands.Add(extractInterfaceCommand);
        rootCommand.Subcommands.Add(extractSoundsCommand);
        rootCommand.Subcommands.Add(extractAllCommand);
        rootCommand.Subcommands.Add(listNamesCommand);
        rootCommand.Subcommands.Add(extractScenarioCommand);

        return await rootCommand.Parse(args).InvokeAsync();
    }
}
