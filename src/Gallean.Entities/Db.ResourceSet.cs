using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [JsonConverter(typeof(ResourceSetJsonConverter))]
    public readonly record struct ResourceSet(int Gold, int DeathMana, int RuneMana, int LifeMana, int InfernalMana, int GroveMana)
    {
        public bool IsEmpty => Gold == 0 && DeathMana == 0 && RuneMana == 0 && LifeMana == 0 && InfernalMana == 0 && GroveMana == 0;
        public static ResourceSet Empty { get; } = new(0, 0, 0, 0, 0, 0);
        public string Value => IsEmpty ? string.Empty : $"g{Gold:D4}:r{InfernalMana:D4}:y{LifeMana:D4}:e{DeathMana:D4}:w{RuneMana:D4}:b{GroveMana:D4}";

        public static ResourceSet Parse(string value)
        {
            (int gold, int deathMana, int runeMana, int lifeMana, int infernalMana, int groveMana) = ParseResourceString(value);
            return new ResourceSet(gold, deathMana, runeMana, lifeMana, infernalMana, groveMana);
        }

        private static (int gold, int deathMana, int runeMana, int lifeMana, int infernalMana, int groveMana) ParseResourceString(string value)
        {
            int gold = 0, deathMana = 0, runeMana = 0, lifeMana = 0, infernalMana = 0, groveMana = 0;
            ReadOnlySpan<char> span = value.AsSpan();

            while (!span.IsEmpty)
            {
                int separatorIndex = span.IndexOf(':');
                ReadOnlySpan<char> entry = separatorIndex >= 0 ? span[..separatorIndex] : span;

                if (!entry.IsEmpty && int.TryParse(entry[1..], out int amount))
                {
                    switch (char.ToLowerInvariant(entry[0]))
                    {
                        case 'g':
                            gold = amount;
                            break;
                        case 'e':
                            deathMana = amount;
                            break;
                        case 'w':
                            runeMana = amount;
                            break;
                        case 'y':
                            lifeMana = amount;
                            break;
                        case 'r':
                            infernalMana = amount;
                            break;
                        case 'b':
                            groveMana = amount;
                            break;
                    }
                }

                if (separatorIndex < 0)
                {
                    break;
                }

                span = span[(separatorIndex + 1)..];
            }

            return (gold, deathMana, runeMana, lifeMana, infernalMana, groveMana);
        }

        public override string ToString()
        {
            return Value;
        }
    }

    internal class ResourceSetJsonConverter : JsonConverter<ResourceSet>
    {
        public override ResourceSet Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? val = reader.GetString();
            return ResourceSet.Parse(val ?? string.Empty);
        }

        public override void Write(Utf8JsonWriter writer, ResourceSet value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }
    }
}
