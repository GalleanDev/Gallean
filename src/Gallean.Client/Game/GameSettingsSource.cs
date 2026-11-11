using Gallean.Configuration;

namespace Gallean.Client;

internal sealed class GameSettingsSource(GameSettings settings)
{
    public GameSettings Current { get; set; } = settings;
}
