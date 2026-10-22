using System.CommandLine;
using System.CommandLine.Parsing;
using Gallean.Configuration;

namespace Packer;

internal static class PackerCli
{
    public static RootCommand Create(PackerOutput output)
    {
        HttpClient httpClient = new(new HttpClientHandler { AllowAutoRedirect = false });
        DownloadStore downloadStore = new(httpClient, output, AppDirectories.Downloads);
        NativeLibraryStore libraryStore = new(new NuGetClient(httpClient), downloadStore, AppContext.BaseDirectory, AppDirectories.Libraries);
        NativeLibraryLoader libraryLoader = new(libraryStore);
        ModelStore modelStore = new(AppContext.BaseDirectory, AppDirectories.EnhanceModels);

        Option<string> outputOption = new("--out", "-o")
        {
            Description = "Output .galleanpack path.",
            Required = true
        };

        RootCommand rootCommand = new("Creates and edits Gallean asset packs for the game client.");
        rootCommand.Options.Add(outputOption);

        rootCommand.SetAction(parseResult =>
        {
            PackCommandOptions options = new(parseResult.GetRequiredValue(outputOption));
            PackCommandHandler handler = new(output, libraryLoader);

            return handler.Run(options);
        });

        rootCommand.Subcommands.Add(CreateSetupEnvCommand(output, httpClient, downloadStore, libraryStore, modelStore));
        rootCommand.Subcommands.Add(CreateEnhanceCommand(output, libraryLoader, modelStore));
        rootCommand.Subcommands.Add(CreatePreviewCommand(output, libraryLoader));

        return rootCommand;
    }

    private static Command CreateSetupEnvCommand(
        PackerOutput output,
        HttpClient httpClient,
        DownloadStore downloadStore,
        NativeLibraryStore libraryStore,
        ModelStore modelStore)
    {
        Option<UmbraDevice> deviceOption = new("--device")
        {
            Description = "Device type for which to setup the environment (CPU or GPU).",
            Required = true
        };

        Option<string?> huggingFaceTokenOption = new("--hf-token")
        {
            Description = "Access token for Hugging Face. Defaults to the HF_TOKEN environment variable.",
            DefaultValueFactory = _ => Environment.GetEnvironmentVariable("HF_TOKEN")
        };

        Command setupEnvCommand = new("setup-env", "Setup the required environment.");

        setupEnvCommand.Options.Add(deviceOption);
        setupEnvCommand.Options.Add(huggingFaceTokenOption);

        setupEnvCommand.SetAction((parseResult, cancellationToken) =>
        {
            SetupEnvCommandOptions options = new(parseResult.GetRequiredValue(deviceOption));
            HuggingFaceClient huggingFaceClient = new(httpClient, parseResult.GetValue(huggingFaceTokenOption));
            ModelDownloader modelDownloader = new(modelStore, huggingFaceClient, downloadStore);
            EnvironmentBuilder environmentBuilder = new(libraryStore, modelDownloader);
            SetupEnvCommandHandler handler = new(output, environmentBuilder);

            return handler.RunAsync(options, cancellationToken);
        });

        return setupEnvCommand;
    }

    private static Command CreateEnhanceCommand(
        PackerOutput output,
        NativeLibraryLoader libraryLoader,
        ModelStore modelStore)
    {
        Option<string> packOption = new("--pack")
        {
            Description = "The .galleanpack file to enhance.",
            Required = true
        };

        Option<string[]> categoryOption = new("--category")
        {
            Description = $"Which entries to process, by glob pattern over canonical ids, e.g. " +
                          $"battle-unit/*/attack/attacker, or by an alias: {string.Join(", ", CategoryAliases.Names)}.",
            AllowMultipleArgumentsPerToken = true,
            DefaultValueFactory = _ => []
        };

        Option<EnhancementType[]> enhancementOption = new("--enhancement")
        {
            Description = "Which enhancements the pack should have (upscale | refine | alpha | surface).",
            HelpName = "enhancements",
            AllowMultipleArgumentsPerToken = true,
            DefaultValueFactory = _ => [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha, EnhancementType.Surface]
        };

        Option<string> cacheDirOption = new("--cache-dir")
        {
            Description = $"Directory with frames computed across runs. Defaults to {AppDirectories.EnhanceCache}.",
            DefaultValueFactory = _ => AppDirectories.EnhanceCache
        };

        Option<bool> cacheOnlyOption = new("--cache-only")
        {
            Description = "Compute and cache the frames without writing an output pack."
        };

        Option<string?> outputOption = new("--out", "-o")
        {
            Description = "Output .galleanpack path."
        };

        Option<bool> sparseOption = new("--sparse")
        {
            Description = "Write entries as an overlay over the input pack."
        };

        Option<UmbraDevice> deviceOption = new("--device")
        {
            Description = "Where to run the models (CPU or GPU).",
            Required = true
        };

        Command enhanceCommand = new("enhance", "Enhance an existing pack.");
        enhanceCommand.Options.Add(packOption);
        enhanceCommand.Options.Add(categoryOption);
        enhanceCommand.Options.Add(enhancementOption);
        enhanceCommand.Options.Add(cacheDirOption);
        enhanceCommand.Options.Add(cacheOnlyOption);
        enhanceCommand.Options.Add(outputOption);
        enhanceCommand.Options.Add(sparseOption);
        enhanceCommand.Options.Add(deviceOption);

        enhanceCommand.SetAction(parseResult =>
        {
            EnhanceCommandOptions options = new(
                parseResult.GetRequiredValue(packOption),
                parseResult.GetRequiredValue(categoryOption),
                parseResult.GetRequiredValue(enhancementOption),
                parseResult.GetRequiredValue(cacheDirOption),
                parseResult.GetValue(cacheOnlyOption),
                parseResult.GetValue(outputOption),
                parseResult.GetValue(sparseOption),
                parseResult.GetRequiredValue(deviceOption));

            EnhanceCommandHandler handler = new(output, libraryLoader, modelStore);

            return handler.Run(options);
        });

        return enhanceCommand;
    }

    private static Command CreatePreviewCommand(PackerOutput output, NativeLibraryLoader libraryLoader)
    {
        Option<string> packOption = new("--pack")
        {
            Description = "The .galleanpack file the cached frames were computed from.",
            Required = true
        };

        Option<string[]> categoryOption = new("--category")
        {
            Description = $"Which entries to process, by glob pattern over canonical ids, e.g. " +
                          $"battle-unit/*/attack/attacker, or by an alias: {string.Join(", ", CategoryAliases.Names)}.",
            AllowMultipleArgumentsPerToken = true,
            DefaultValueFactory = _ => []
        };

        Option<string> cacheDirOption = new("--cache-dir")
        {
            Description = $"Directory with frames computed across runs. Defaults to {AppDirectories.EnhanceCache}.",
            DefaultValueFactory = _ => AppDirectories.EnhanceCache
        };

        Option<UmbraDevice> deviceOption = new("--device")
        {
            Description = "Where the models were run (CPU or GPU).",
            Required = true
        };

        Option<PreviewTransformType?> transformOption = new("--transform")
        {
            Description = "Which cached transform to read. Defaults to the last transform for a frame."
        };

        Option<uint> framesPerSecondOption = new("--fps")
        {
            Description = "How many frames to render per second.",
            DefaultValueFactory = _ => 12,
            Validators =
            {
                option =>
                {
                    foreach (Token token in option.Tokens)
                    {
                        if (uint.TryParse(token.Value, out uint framesPerSecond) && framesPerSecond == 0)
                        {
                            option.AddError("The --fps must be greater than zero.");
                        }
                    }
                }
            }
        };

        Option<string> outputOption = new("--out", "-o")
        {
            Description = "Output directory or a single file path when one entry is selected.",
            Required = true
        };

        Command previewCommand = new("preview", "Build animated preview for an entry.");
        previewCommand.Options.Add(packOption);
        previewCommand.Options.Add(categoryOption);
        previewCommand.Options.Add(cacheDirOption);
        previewCommand.Options.Add(deviceOption);
        previewCommand.Options.Add(transformOption);
        previewCommand.Options.Add(framesPerSecondOption);
        previewCommand.Options.Add(outputOption);

        previewCommand.SetAction(parseResult =>
        {
            PreviewCommandOptions options = new(
                parseResult.GetRequiredValue(packOption),
                parseResult.GetRequiredValue(categoryOption),
                parseResult.GetRequiredValue(cacheDirOption),
                parseResult.GetRequiredValue(deviceOption),
                parseResult.GetValue(transformOption),
                parseResult.GetRequiredValue(outputOption),
                parseResult.GetRequiredValue(framesPerSecondOption));

            PreviewCommandHandler handler = new(output, libraryLoader);

            return handler.Run(options);
        });

        return previewCommand;
    }
}
