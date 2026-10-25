using System.CommandLine;
using System.Text.Json;

namespace Extractor;

internal class ExtractInterfaceCommand : Command
{
    public ExtractInterfaceCommand(Option<string> gamePathOption, Option<string> outputOption)
        : base("extract-interface", "Extracts user interface resources.")
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
        GameFiles gameFiles = new(gamePath);

        string outputPath = Path.Combine(output, "Interface");
        Directory.CreateDirectory(outputPath);

        EnumerationOptions enumerationOptions = new()
        {
            MatchCasing = MatchCasing.CaseInsensitive
        };

        List<string> dlgFiles = [];
        if (Directory.Exists(gameFiles.InterfaceRoot))
        {
            dlgFiles.AddRange(Directory.EnumerateFiles(gameFiles.InterfaceRoot, "*.dlg", enumerationOptions));
        }

        Console.WriteLine($"Found {dlgFiles.Count} interface resource files.");

        foreach (string dlgFile in dlgFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string fileName = Path.GetFileNameWithoutExtension(dlgFile);
            Console.WriteLine($"  Loading {fileName}...");

            SceneInterfaceExtractor extractor = SceneInterfaceExtractor.Load(dlgFile);

            IReadOnlyCollection<SceneInterface> interfaces = extractor.GetAllSceneInterfaces();

            string currentOutputPath = Path.Combine(outputPath, $"{fileName}.json");
            await using FileStream stream = File.Create(currentOutputPath);
            await JsonSerializer.SerializeAsync(stream, interfaces, ExtractorJsonSerializerContext.Default.SceneInterfaceCollection, cancellationToken);

            Console.WriteLine($"  {fileName}: {interfaces.Count} interfaces extracted successfully.");
        }

        Console.WriteLine($"Interface extraction completed -> {outputPath}");
    }
}
