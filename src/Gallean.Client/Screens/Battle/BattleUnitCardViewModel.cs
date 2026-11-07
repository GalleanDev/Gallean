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
    AnimationRange HitpointsAnimation,
    IReadOnlyList<UnitStatusViewModel> Statuses)
{
    public string LevelText => Tr("Lv. {0}", Level);
}
