using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text;
using System.Text.Json;
using DbfDataReader;

namespace Extractor;

internal class ExtractDatabaseCommand : Command
{
    public const int DefaultCodePage = 866;

    private const string ScenarioColumn = "NO_SCEN";

    private static readonly string[] ScenarioRootTables = ["Sinfo.json"];

    public ExtractDatabaseCommand(Option<string> gamePathOption, Option<string> outputOption)
        : base("extract-database", "Extracts database resources.")
    {
        Option<int> codePageOption = new("--code-page")
        {
            Description = "Code page of the database",
            DefaultValueFactory = _ => DefaultCodePage,
            Validators =
            {
                option =>
                {
                    foreach (Token token in option.Tokens)
                    {
                        if (int.TryParse(token.Value, out int codePage) && TryGetEncoding(codePage) is null)
                        {
                            option.AddError($"Unknown code page '{token.Value}'.");
                        }
                    }
                }
            }
        };

        Options.Add(gamePathOption);
        Options.Add(codePageOption);

        SetAction(async (parseResult, cancellationToken) =>
        {
            string gamePath = parseResult.GetRequiredValue(gamePathOption);
            string output = parseResult.GetRequiredValue(outputOption);
            int codePage = parseResult.GetRequiredValue(codePageOption);
            await ExecuteAsync(gamePath, output, GetEncoding(codePage), cancellationToken);
        });
    }

    internal static async Task ExecuteAsync(string gamePath, string output, Encoding encoding, CancellationToken cancellationToken = default)
    {
        string outputPath = Path.Combine(output, "Database");
        Directory.CreateDirectory(outputPath);

        Console.WriteLine($"Extracting database resources from '{gamePath}'...");

        DbfDataReaderOptions dbfOptions = new()
        {
            Encoding = encoding,
            SkipDeletedRecords = true,
            StringTrimming = StringTrimmingOption.TrimEnd
        };

        EnumerationOptions enumerationOptions = new()
        {
            RecurseSubdirectories = true,
            MatchCasing = MatchCasing.CaseInsensitive
        };

        foreach (string file in Directory.EnumerateFiles(gamePath, "*.dbf", enumerationOptions))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string relativePath = Path.GetRelativePath(gamePath, file);
            Console.WriteLine($"Processing '{relativePath}'...");

            List<Dictionary<string, object?>> records = [];

            await using (DbfDataReader.DbfDataReader reader = new(file, dbfOptions))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    Dictionary<string, object?> record = [];

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string columnName = reader.GetName(i);
                        record[columnName] = GetValue(reader, i);
                    }

                    records.Add(record);
                }
            }

            if (records.Count > 0)
            {
                string relativeDir = Path.GetDirectoryName(relativePath) ?? string.Empty;
                string fileName = $"{Path.GetFileNameWithoutExtension(file)}.json";
                string finalOutputPath = Path.Combine(outputPath, relativeDir);
                Directory.CreateDirectory(finalOutputPath);

                if (IsPerScenarioTable(fileName, records))
                {
                    WritePerScenarioRecords(finalOutputPath, fileName, records);
                }
                else
                {
                    WriteRecordsFile(Path.Combine(finalOutputPath, fileName), records);
                }
            }
        }

        Console.WriteLine($"Database extraction completed -> {outputPath}");
    }

    internal static Encoding GetEncoding(int codePage)
    {
        Encoding? encoding = TryGetEncoding(codePage);

        if (encoding is null)
        {
            throw new InvalidDataException($"Unknown code page '{codePage}'.");
        }

        return encoding;
    }

    private static Encoding? TryGetEncoding(int codePage)
    {
        if (codePage is >= 0 and <= 65535)
        {
            return CodePagesEncodingProvider.Instance.GetEncoding(codePage);
        }
        else
        {
            return null;
        }
    }

    private static object? GetValue(DbfDataReader.DbfDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }
        else
        {
            object value = reader.GetValue(ordinal);

            if (value is string str)
            {
                string trimmed = str.TrimEnd();

                if (trimmed.Length > 0)
                {
                    return trimmed;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return value;
            }
        }
    }

    private static bool IsPerScenarioTable(string fileName, List<Dictionary<string, object?>> records)
    {
        if (ScenarioRootTables.Contains(fileName, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }
        else
        {
            return records[0].ContainsKey(ScenarioColumn);
        }
    }

    private static void WritePerScenarioRecords(string directory, string fileName, List<Dictionary<string, object?>> records)
    {
        Dictionary<string, List<Dictionary<string, object?>>> recordsByScenario = [];
        List<string> scenarioOrder = [];

        foreach (Dictionary<string, object?> record in records)
        {
            if (record[ScenarioColumn] is not string scenarioId)
            {
                throw new InvalidOperationException($"Table '{fileName}' has a record without a '{ScenarioColumn}' value.");
            }

            if (scenarioId.Length <= 0)
            {
                throw new InvalidOperationException($"Table '{fileName}' specifies an empty scenario ID.");
            }

            if (!recordsByScenario.TryGetValue(scenarioId, out List<Dictionary<string, object?>>? scenarioRecords))
            {
                scenarioRecords = [];
                recordsByScenario[scenarioId] = scenarioRecords;
                scenarioOrder.Add(scenarioId);
            }

            scenarioRecords.Add(record);
        }

        foreach (string scenarioId in scenarioOrder)
        {
            string scenarioDirectory = Path.Combine(directory, scenarioId);
            Directory.CreateDirectory(scenarioDirectory);

            WriteRecordsFile(Path.Combine(scenarioDirectory, fileName), recordsByScenario[scenarioId]);
        }
    }

    private static void WriteRecordsFile(string path, List<Dictionary<string, object?>> records)
    {
        DbFileWriter.Write(path, writer => WriteRecords(writer, records));
    }

    private static void WriteRecords(Utf8JsonWriter writer, List<Dictionary<string, object?>> records)
    {
        foreach (Dictionary<string, object?> record in records)
        {
            writer.WriteStartObject();

            foreach (KeyValuePair<string, object?> field in record)
            {
                writer.WritePropertyName(field.Key);
                WriteValue(writer, field.Value);
            }

            writer.WriteEndObject();
        }
    }

    private static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string text:
                writer.WriteStringValue(text);
                break;
            case bool flag:
                writer.WriteBooleanValue(flag);
                break;
            case int number:
                writer.WriteNumberValue(number);
                break;
            case long number:
                writer.WriteNumberValue(number);
                break;
            case short number:
                writer.WriteNumberValue(number);
                break;
            case byte number:
                writer.WriteNumberValue(number);
                break;
            case decimal number:
                writer.WriteNumberValue(number);
                break;
            case double number:
                writer.WriteNumberValue(number);
                break;
            case float number:
                writer.WriteNumberValue(number);
                break;
            case DateTime date:
                writer.WriteStringValue(date);
                break;
            default:
                writer.WriteStringValue(value.ToString());
                break;
        }
    }
}
