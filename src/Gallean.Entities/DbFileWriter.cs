using System.Text.Json;

namespace Gallean.Entities;

public static class DbFileWriter
{
    public static void Write(string path, Action<Utf8JsonWriter> writeRows)
    {
        using FileStream stream = File.Create(path);
        using Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteNumber("formatVersion", DbFile.CurrentFormatVersion);
        writer.WriteStartArray("rows");

        writeRows(writer);

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
