namespace Gallean.Engine;

[Flags]
public enum LeaderAbility
{
    None = 0,
    Zealot = 1 << 0,
    Incorruptible = 1 << 1,
    WeaponMaster = 1 << 2,
    WandScrollUse = 1 << 3,
    WeaponArmorUse = 1 << 4,
    BannerUse = 1 << 5,
    JewelryUse = 1 << 6,
    Rod = 1 << 7,
    OrbUse = 1 << 8,
    TalismanUse = 1 << 9,
    TravelItemUse = 1 << 10,
    CriticalHit = 1 << 11
}
