using Gallean.Configuration;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed record LeaderImportLeaderViewModel(
    LeaderEntry Entry,
    AssetSpriteKey.UnitPortrait PortraitKey,
    string Name,
    string LevelText,
    string BattlesWonText,
    IReadOnlyList<AssetSpriteKey.ItemIcon> ItemIcons,
    bool IsAvailable);
