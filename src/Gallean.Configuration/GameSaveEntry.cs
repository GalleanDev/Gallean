using Gallean.Common;

namespace Gallean.Configuration;

public sealed record GameSaveEntry(GameSaveId Id, Result<GameSaveMetadata> Metadata);
