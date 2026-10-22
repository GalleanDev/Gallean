using System.CommandLine;

namespace Extractor;

internal class ListNamesCommand : Command
{
    public ListNamesCommand(Option<string> gamePathOption, Option<string> outputOption)
        : base("list-names", "Lists stored file, logical image, and animation names of image archives.")
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
        string outputPath = Path.Combine(output, "Names");
        Directory.CreateDirectory(outputPath);

        GameFiles gameFiles = new(gamePath);
        List<string> ffFiles = [];

        foreach (string path in (string[])[gameFiles.ImagesRoot, gameFiles.InterfaceRoot, gameFiles.ScenarioDataRoot])
        {
            if (Directory.Exists(path))
            {
                ffFiles.AddRange(Directory.EnumerateFiles(path, "*.ff"));
            }
        }

        Console.WriteLine($"Found {ffFiles.Count} image resource files.");

        foreach (string ffFile in ffFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string fileName = Path.GetFileNameWithoutExtension(ffFile);
            ImagesExtractor extractor = ImagesExtractor.Load(ffFile);

            List<string> lines = [];

            foreach (string name in extractor.GetAllFilesNames().OrderBy(name => name, StringComparer.Ordinal))
            {
                lines.Add($"file\t{name}");
            }

            foreach (string name in extractor.GetAllImageNames().OrderBy(name => name, StringComparer.Ordinal))
            {
                lines.Add($"image\t{name}");
            }

            foreach (string name in extractor.GetAllAnimationNames().OrderBy(name => name, StringComparer.Ordinal))
            {
                lines.Add($"anim\t{name}");
            }

            string targetPath = Path.Combine(outputPath, $"{fileName}.txt");
            await File.WriteAllLinesAsync(targetPath, lines, cancellationToken);
            Console.WriteLine($"  {fileName}: {lines.Count} names.");
        }

        Console.WriteLine($"Name listing completed -> {outputPath}");
    }
}
