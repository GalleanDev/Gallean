using System.CommandLine;

namespace Extractor;

internal class ExtractAllCommand : Command
{
    public ExtractAllCommand(Option<string> gamePathOption, Option<string> outputOption)
        : base("extract-all", "Extracts all available resources.")
    {
        Options.Add(gamePathOption);

        SetAction(async (parseResult, cancellationToken) =>
        {
            string gamePath = parseResult.GetRequiredValue(gamePathOption);
            string output = parseResult.GetRequiredValue(outputOption);
            await ExecuteAsync(gamePath, output, cancellationToken);
        });
    }

    internal static async Task ExecuteAsync(string gamePath, string output, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Extracting all resources from '{gamePath}'...");
        Console.WriteLine();

        await ExtractDatabaseCommand.ExecuteAsync(gamePath, output, cancellationToken);
        await ExtractImagesCommand.ExecuteAsync(gamePath, output, cancellationToken);
        await ExtractInterfaceCommand.ExecuteAsync(gamePath, output, cancellationToken);
        await ExtractSoundsCommand.ExecuteAsync(gamePath, output, cancellationToken);

        Console.WriteLine();
        Console.WriteLine("Extraction completed.");
    }
}
