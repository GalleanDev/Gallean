using System.Text.Json;
using System.Text.Json.Serialization;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

internal sealed class WorldUnitGridJsonConverter : JsonConverter<UnitGrid<WorldUnit>>
{
    internal sealed record UnitGridEntryJson(WorldUnit Unit, int Line, int Flank, bool IsLarge);

    public override UnitGrid<WorldUnit> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        UnitGridEntryJson[] entries =
            WorldStateJsonSerializerContext.Default.UnitGridEntryJsonArray.DeserializeRequired(ref reader);

        UnitGrid<WorldUnit>.Builder builder = new();

        foreach (UnitGridEntryJson entry in entries)
        {
            GridPosition position = new(entry.Line, entry.Flank);

            if (!UnitGrid<WorldUnit>.IsValidPosition(position))
            {
                throw new JsonException("Invalid unit grid position.");
            }

            if (!builder.TryPlace(entry.Unit, position, entry.IsLarge))
            {
                throw new JsonException("Invalid unit grid position.");
            }
        }

        return builder.Build();
    }

    public override void Write(Utf8JsonWriter writer, UnitGrid<WorldUnit> value, JsonSerializerOptions options)
    {
        UnitGrid<WorldUnit> overlayGrid = value.GetOverlayGrid();

        if (!overlayGrid.IsEmpty)
        {
            throw new JsonException("Unexpected unit grid overlay.");
        }

        UnitGridEntryJson[] entries =
        [
            .. value.GetUnits().Select(entry => new UnitGridEntryJson(
                entry.Unit,
                entry.Position.Line,
                entry.Position.Flank,
                value.IsLargeAt(entry.Position)))
        ];

        JsonSerializer.Serialize(writer, entries, WorldStateJsonSerializerContext.Default.UnitGridEntryJsonArray);
    }
}
