using System.Text.Json;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

public static class WorldStateJsonSerializer
{
    public static async Task SerializeAsync(
        Stream stream,
        WorldState state,
        CancellationToken cancellationToken = default)
    {
        await JsonSerializer.SerializeAsync(
            stream,
            state,
            WorldStateJsonSerializerContext.Default.WorldState,
            cancellationToken);
    }

    public static async Task<WorldState> DeserializeAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        return await WorldStateJsonSerializerContext.Default.WorldState.DeserializeRequiredAsync(stream, cancellationToken);
    }
}
