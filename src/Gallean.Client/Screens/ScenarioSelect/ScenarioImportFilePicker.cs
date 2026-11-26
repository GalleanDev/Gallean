using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Gallean.Client;

internal static class ScenarioImportFilePicker
{
    public static async Task<IReadOnlyList<ScenarioImportFile>> PickAsync(
        Control control,
        string title,
        string fileTypeName,
        IReadOnlyList<string> patterns)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(control);

        if (topLevel is not null)
        {
            FilePickerOpenOptions options = new()
            {
                Title = title,
                AllowMultiple = true,
                FileTypeFilter = [new FilePickerFileType(fileTypeName) { Patterns = patterns }]
            };

            IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
            List<ScenarioImportFile> result = [];

            foreach (IStorageFile file in files)
            {
                result.Add(await ReadFileAsync(file));
            }

            return result;
        }
        else
        {
            return [];
        }
    }

    private static async Task<ScenarioImportFile> ReadFileAsync(IStorageFile file)
    {
        await using Stream stream = await file.OpenReadAsync();
        using MemoryStream data = new();

        await stream.CopyToAsync(data);

        return new ScenarioImportFile(file.Name, data.ToArray());
    }
}
