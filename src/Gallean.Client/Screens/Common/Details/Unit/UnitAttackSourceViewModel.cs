using Avalonia.Media;

namespace Gallean.Client;

internal sealed record UnitAttackSourceViewModel(IconMoniker Icon, string Text, Color Color)
{
    public static UnitAttackSourceViewModel Create(UnitAttackSource source)
    {
        return source switch
        {
            UnitAttackSource.Weapon => new UnitAttackSourceViewModel(IconMoniker.AttackSourceWeapon, UnitAttackFormatter.FormatSource(source), Theme.AttackSource.WeaponColor),
            UnitAttackSource.Mind => new UnitAttackSourceViewModel(IconMoniker.AttackSourceMind, UnitAttackFormatter.FormatSource(source), Theme.AttackSource.MindColor),
            UnitAttackSource.Life => new UnitAttackSourceViewModel(IconMoniker.AttackSourceLife, UnitAttackFormatter.FormatSource(source), Theme.AttackSource.LifeColor),
            UnitAttackSource.Death => new UnitAttackSourceViewModel(IconMoniker.AttackSourceDeath, UnitAttackFormatter.FormatSource(source), Theme.AttackSource.DeathColor),
            UnitAttackSource.Fire => new UnitAttackSourceViewModel(IconMoniker.AttackSourceFire, UnitAttackFormatter.FormatSource(source), Theme.AttackSource.FireColor),
            UnitAttackSource.Water => new UnitAttackSourceViewModel(IconMoniker.AttackSourceWater, UnitAttackFormatter.FormatSource(source), Theme.AttackSource.WaterColor),
            UnitAttackSource.Earth => new UnitAttackSourceViewModel(IconMoniker.AttackSourceEarth, UnitAttackFormatter.FormatSource(source), Theme.AttackSource.EarthColor),
            UnitAttackSource.Air => new UnitAttackSourceViewModel(IconMoniker.AttackSourceAir, UnitAttackFormatter.FormatSource(source), Theme.AttackSource.AirColor)
        };
    }
}
