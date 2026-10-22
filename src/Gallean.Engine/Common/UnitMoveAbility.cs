namespace Gallean.Engine;

[Flags]
public enum UnitMoveAbility
{
    None = 0,
    Forest = 1 << 0,
    Water = 1 << 1,
    Flying = Forest | Water
}
