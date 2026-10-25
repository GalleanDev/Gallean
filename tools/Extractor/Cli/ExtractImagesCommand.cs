using System.CommandLine;
using ImageMagick;

namespace Extractor;

internal class ExtractImagesCommand : Command
{
    public ExtractImagesCommand(Option<string> gamePathOption, Option<string> outputOption)
        : base("extract-images", "Extracts image resources.")
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

        string outputPath = Path.Combine(output, "Images");
        Directory.CreateDirectory(outputPath);

        EnumerationOptions enumerationOptions = new()
        {
            MatchCasing = MatchCasing.CaseInsensitive
        };

        List<string> ffFiles = [];
        if (Directory.Exists(gameFiles.ImagesRoot))
        {
            ffFiles.AddRange(Directory.EnumerateFiles(gameFiles.ImagesRoot, "*.ff", enumerationOptions));
        }

        Console.WriteLine($"Found {ffFiles.Count} image resource files.");

        foreach (string ffFile in ffFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string fileName = Path.GetFileNameWithoutExtension(ffFile);
            Console.WriteLine($"  Loading {fileName}...");

            ImagesExtractor extractor = ImagesExtractor.Load(ffFile);

            string currentOutputPath = Path.Combine(outputPath, fileName);
            Directory.CreateDirectory(currentOutputPath);

            int saved = 0;
            foreach (string name in extractor.GetAllFilesNames())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    string targetPath = Path.Combine(currentOutputPath, name);

                    RawBitmap bitmap = extractor.GetImage(name);
                    PixelReadSettings settings = new((uint)bitmap.OriginalWidth, (uint)bitmap.OriginalHeight, StorageType.Char, PixelMapping.RGBA);

                    using MagickImage image = new(bitmap.Data, settings);
                    image.Format = MagickFormat.Bmp;

                    await image.WriteAsync(targetPath, cancellationToken);

                    saved++;
                }
                else
                {
                    Console.WriteLine($"  Skipped extracting '{name}'.");
                }
            }

            Console.WriteLine($"  {fileName}: {saved} extracted successfully.");
        }

        Console.WriteLine($"Image extraction completed -> {outputPath}");
    }
}
