using Avalonia;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed record BattleLight(
    Point Position,
    double Standoff,
    double Range,
    Color Color,
    double Power,
    Vector Sway,
    Vector SwayRate,
    Vector SwayPhase,
    double Flicker,
    double FlickerRate)
{
    public static BattleLight MakeLantern(Point position, double range, double power)
    {
        return new BattleLight(
            position,
            Standoff: 70,
            range,
            Theme.Battle.Light.LanternColor,
            power,
            Sway: new Vector(7, 4),
            SwayRate: new Vector(3, 5),
            SwayPhase: new Vector(position.X * 0.037, position.Y * 0.053),
            Flicker: power * 0.22,
            FlickerRate: 9);
    }

    public static BattleLight MakeGhostCrystal(Point position, double range, double power)
    {
        return new BattleLight(
            position,
            Standoff: 60,
            range,
            Theme.Battle.Light.GhostCrystalColor,
            power,
            Sway: new Vector(0, 0),
            SwayRate: new Vector(0, 0),
            SwayPhase: new Vector(position.X * 0.041, position.Y * 0.029),
            Flicker: power * 0.12,
            FlickerRate: 3);
    }

    public static BattleLight MakeWindow(Point position, double range, double power)
    {
        return new BattleLight(
            position,
            Standoff: 70,
            range,
            Theme.Battle.Light.WindowColor,
            power,
            Sway: new Vector(0, 0),
            SwayRate: new Vector(0, 0),
            SwayPhase: new Vector(0, 0),
            Flicker: 0,
            FlickerRate: 0);
    }

    public static BattleLight MakeLava(Point position, double range, double power)
    {
        return new BattleLight(
            position,
            Standoff: 40,
            range,
            Theme.Battle.Light.LavaColor,
            power,
            Sway: new Vector(12, 5),
            SwayRate: new Vector(1, 2),
            SwayPhase: new Vector(position.X * 0.031, position.Y * 0.047),
            Flicker: power * 0.18,
            FlickerRate: 2);
    }

    public static BattleLight MakeGlowingWater(Point position, double range, double power)
    {
        return new BattleLight(
            position,
            Standoff: 30,
            range,
            Theme.Battle.Light.GlowingWaterColor,
            power,
            Sway: new Vector(0, 0),
            SwayRate: new Vector(0, 0),
            SwayPhase: new Vector(position.X * 0.043, position.Y * 0.037),
            Flicker: power * 0.1,
            FlickerRate: 3);
    }
}
