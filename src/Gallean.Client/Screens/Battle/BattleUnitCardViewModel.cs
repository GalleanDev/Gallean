namespace Gallean.Client;

internal sealed record BattleUnitCardViewModel(
    BattleUnitId UnitId,
    BattleSide Side,
    AssetSpriteKey.UnitEventPortrait PortraitKey,
    AssetSpriteKey.UnitPortrait IconKey,
    string Name,
    int Level,
    int Hitpoints,
    int MaxHitpoints,
    IReadOnlyList<AnimationProperty> HitpointsAnimation,
    IReadOnlyList<UnitStatusViewModel> Statuses);
