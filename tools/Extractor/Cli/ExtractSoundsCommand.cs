using System.CommandLine;
using System.Text.Json;

namespace Extractor;

internal class ExtractSoundsCommand : Command
{
    public ExtractSoundsCommand(Option<string> gamePathOption, Option<string> outputOption)
        : base("extract-sounds", "Extracts sound resources.")
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

        string outputPath = Path.Combine(output, "Sounds");
        Directory.CreateDirectory(outputPath);

        foreach (string wdbFile in Directory.EnumerateFiles(gameFiles.SoundsRoot, "*.wdb"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string fileName = Path.GetFileNameWithoutExtension(wdbFile);
            Console.WriteLine($"  Loading {fileName}...");

            SoundsExtractor extractor = SoundsExtractor.Load(wdbFile);

            string currentOutputPath = Path.Combine(outputPath, fileName);
            Directory.CreateDirectory(currentOutputPath);

            int saved = 0;
            foreach (string name in extractor.GetAllFilesNames())
            {
                cancellationToken.ThrowIfCancellationRequested();

                byte[]? sound = extractor.GetSound(name);
                if (sound is not null)
                {
                    string targetFile = Path.Combine(currentOutputPath, name);
                    await File.WriteAllBytesAsync(targetFile, sound, cancellationToken);
                    saved++;
                }
                else
                {
                    Console.WriteLine($"  Skipped extracting '{name}'.");
                }
            }

            Console.WriteLine($"  {fileName}: {saved} extracted successfully.");
        }

        foreach (string wdtFile in Directory.EnumerateFiles(gameFiles.SoundsRoot, "*.wdt"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string fileName = Path.GetFileNameWithoutExtension(wdtFile);
            Console.WriteLine($"  Loading {fileName}...");

            SoundsMappingExtractor mappingExtractor = SoundsMappingExtractor.Load(wdtFile);

            IReadOnlyDictionary<string, UnitTypeSounds> mappings = mappingExtractor.GetAllUnitTypeSounds();

            string targetFile = Path.Combine(outputPath, $"{fileName}.json");
            await using FileStream stream = File.Create(targetFile);
            await JsonSerializer.SerializeAsync(stream, mappings, ExtractorJsonSerializerContext.Default.UnitTypeSoundsMap, cancellationToken);

            Console.WriteLine($"  {fileName}: {mappings.Count} sound mappings extracted successfully.");
        }

        Console.WriteLine($"Sound extraction completed -> {outputPath}");
    }
}
