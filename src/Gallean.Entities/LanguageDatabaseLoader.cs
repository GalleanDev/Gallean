using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Gallean.Entities;

public static class LanguageDatabaseLoader
{
    private static readonly DbJsonSerializerContext Context = CreateContext();

    public static LanguageDatabase LoadIndex(string globalsDirectory, string scenariosDirectory)
    {
        return new LanguageDatabase(
            ReadOptional(Path.Combine(globalsDirectory, "Tglobal.json"), Context.TextGlobalListInfo),
            ReadOptional(Path.Combine(globalsDirectory, "Tleader.json"), Context.TextLeaderListInfo),
            ReadOptional(Path.Combine(scenariosDirectory, "Sinfo.json"), Context.ScenarioInfoListInfo),
            []);
    }

    public static LanguageDatabase Load(string globalsDirectory, string scenariosDirectory, Db.ScenarioId scenarioId)
    {
        LanguageDatabase index = LoadIndex(globalsDirectory, scenariosDirectory);
        string scenarioTextsPath = Path.Combine(scenariosDirectory, scenarioId.Value, "Tscen.json");

        return index with { TextScenarios = ReadOptional(scenarioTextsPath, Context.TextScenarioListInfo) };
    }

    private static DbJsonSerializerContext CreateContext()
    {
        JsonSerializerOptions options = new(DbJsonSerializerContext.Default.Options);
        options.Converters.Add(new Db.IdJsonConverterFactory());

        return new DbJsonSerializerContext(options);
    }

    private static List<T> ReadOptional<T>(string path, JsonTypeInfo<List<T>> typeInfo)
    {
        if (File.Exists(path))
        {
            return DbFileReader.Read(path, typeInfo);
        }
        else
        {
            return [];
        }
    }
}
