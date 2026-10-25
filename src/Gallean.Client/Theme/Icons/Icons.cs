using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Gallean.Client;

internal static partial class Icons
{
    public static IconGeometry Get(IconMoniker moniker)
    {
        return moniker switch
        {
            IconMoniker.AbilityBannerUse => AbilityBannerUseIcon,
            IconMoniker.AbilityCriticalHit => AbilityCriticalHitIcon,
            IconMoniker.AbilityIncorruptible => AbilityIncorruptibleIcon,
            IconMoniker.AbilityJewelryUse => AbilityJewelryUseIcon,
            IconMoniker.AbilityOrbUse => AbilityOrbUseIcon,
            IconMoniker.AbilityRod => AbilityRodIcon,
            IconMoniker.AbilityTalismanUse => AbilityTalismanUseIcon,
            IconMoniker.AbilityTravelItemUse => AbilityTravelItemUseIcon,
            IconMoniker.AbilityWandScrollUse => AbilityWandScrollUseIcon,
            IconMoniker.AbilityWeaponArmorUse => AbilityWeaponArmorUseIcon,
            IconMoniker.AbilityWeaponMaster => AbilityWeaponMasterIcon,
            IconMoniker.AbilityZealot => AbilityZealotIcon,
            IconMoniker.Accuracy => AccuracyIcon,
            IconMoniker.Armor => ArmorIcon,
            IconMoniker.Artifact => ArtifactIcon,
            IconMoniker.AttackSourceAir => AttackSourceAirIcon,
            IconMoniker.AttackSourceDeath => AttackSourceDeathIcon,
            IconMoniker.AttackSourceEarth => AttackSourceEarthIcon,
            IconMoniker.AttackSourceFire => AttackSourceFireIcon,
            IconMoniker.AttackSourceLife => AttackSourceLifeIcon,
            IconMoniker.AttackSourceMind => AttackSourceMindIcon,
            IconMoniker.AttackSourceWater => AttackSourceWaterIcon,
            IconMoniker.AttackSourceWeapon => AttackSourceWeaponIcon,
            IconMoniker.Banner => BannerIcon,
            IconMoniker.BattleItem => BattleItemIcon,
            IconMoniker.Blister => AttackSourceFireIcon,
            IconMoniker.Boot => BootIcon,
            IconMoniker.BuildingHub => BuildingHubIcon,
            IconMoniker.Capital => CapitalIcon,
            IconMoniker.Check => CheckIcon,
            IconMoniker.Close => CloseIcon,
            IconMoniker.Collapse => CollapseIcon,
            IconMoniker.Computer => ComputerIcon,
            IconMoniker.Cure => CureIcon,
            IconMoniker.Damage => AttackSourceWeaponIcon,
            IconMoniker.Defend => DefendIcon,
            IconMoniker.Delete => DeleteIcon,
            IconMoniker.DestroyRod => DestroyRodIcon,
            IconMoniker.EndTurn => EndTurnIcon,
            IconMoniker.Exchange => ExchangeIcon,
            IconMoniker.Exit => ExitIcon,
            IconMoniker.Expand => ExpandIcon,
            IconMoniker.Experience => ExperienceIcon,
            IconMoniker.Fortification => FortificationIcon,
            IconMoniker.Frostbite => FrostbiteIcon,
            IconMoniker.Gold => GoldIcon,
            IconMoniker.Heal => HealIcon,
            IconMoniker.Hitpoint => HitpointIcon,
            IconMoniker.Human => HumanIcon,
            IconMoniker.Immunity => DefendIcon,
            IconMoniker.IncreaseDamage => IncreaseDamageIcon,
            IconMoniker.Initiative => InitiativeIcon,
            IconMoniker.Leadership => LeadershipIcon,
            IconMoniker.LearnAbility => LearnAbilityIcon,
            IconMoniker.Locked => LockedIcon,
            IconMoniker.ManaDeath => ManaDeathIcon,
            IconMoniker.ManaGrove => ManaGroveIcon,
            IconMoniker.ManaInfernal => ManaInfernalIcon,
            IconMoniker.ManaLife => ManaLifeIcon,
            IconMoniker.ManaRune => ManaRuneIcon,
            IconMoniker.Menu => MenuIcon,
            IconMoniker.Minus => MinusIcon,
            IconMoniker.Movement => MovementIcon,
            IconMoniker.Next => NextIcon,
            IconMoniker.OrnamentCurl => OrnamentCurlIcon,
            IconMoniker.OrnamentFinial => OrnamentFinialIcon,
            IconMoniker.OrnamentFinialSmall => OrnamentFinialSmallIcon,
            IconMoniker.OrnamentSpear => OrnamentSpearIcon,
            IconMoniker.Paralyze => ParalyzeIcon,
            IconMoniker.Petrify => AttackSourceEarthIcon,
            IconMoniker.PlantRod => PlantRodIcon,
            IconMoniker.Plus => PlusIcon,
            IconMoniker.Poison => PoisonIcon,
            IconMoniker.Previous => PreviousIcon,
            IconMoniker.Quest => QuestIcon,
            IconMoniker.ReduceDamage => ReduceDamageIcon,
            IconMoniker.ReduceInitiative => ReduceInitiativeIcon,
            IconMoniker.ReduceLevel => ReduceLevelIcon,
            IconMoniker.Research => ResearchIcon,
            IconMoniker.Resurrect => ResurrectIcon,
            IconMoniker.Retreat => RetreatIcon,
            IconMoniker.SaveGame => SaveGameIcon,
            IconMoniker.ShatterArmor => ShatterArmorIcon,
            IconMoniker.Skip => SkipIcon,
            IconMoniker.Spellbook => SpellbookIcon,
            IconMoniker.Stack => StackIcon,
            IconMoniker.Target => TargetIcon,
            IconMoniker.TemporaryModifier => TemporaryModifierIcon,
            IconMoniker.ThiefAssassinate => ThiefAssassinateIcon,
            IconMoniker.ThiefBribe => ThiefBribeIcon,
            IconMoniker.ThiefCounterfeitOrders => ThiefCounterfeitOrdersIcon,
            IconMoniker.ThiefDuel => ThiefDuelIcon,
            IconMoniker.ThiefPoisonCity => ThiefPoisonCityIcon,
            IconMoniker.ThiefPoisonStack => ThiefPoisonStackIcon,
            IconMoniker.ThiefRiotCity => ThiefRiotCityIcon,
            IconMoniker.ThiefSpy => ThiefSpyIcon,
            IconMoniker.ThiefSpyRuin => ThiefSpyRuinIcon,
            IconMoniker.ThiefStealGold => ThiefStealGoldIcon,
            IconMoniker.ThiefStealItem => ThiefStealItemIcon,
            IconMoniker.ThiefStealMage => ThiefStealMageIcon,
            IconMoniker.ThiefStealMerchant => ThiefStealMerchantIcon,
            IconMoniker.ThiefStealSpell => ThiefStealSpellIcon,
            IconMoniker.Tome => TomeIcon,
            IconMoniker.Transform => TransformIcon,
            IconMoniker.Unknown => UnknownIcon,
            IconMoniker.Vision => VisionIcon,
            IconMoniker.Wait => WaitIcon,
            IconMoniker.Ward => WardIcon,
            IconMoniker.Warning => WarningIcon
        };
    }

    private static IconGeometry CreateCompound(double viewBox, Point anchor, FillRule fillRule, params string[] paths)
    {
        GeometryGroup group = new() { FillRule = fillRule };

        foreach (string path in paths)
        {
            group.Children.Add(Geometry.Parse(path));
        }

        return new IconGeometry([new IconLayer(group, null)], viewBox, anchor);
    }

    private static IconGeometry CreateColored(double viewBox, IReadOnlyList<Geometry> shapes, params IImmutableBrush[] brushes)
    {
        Debug.Assert(shapes.Count == brushes.Length);

        List<IconLayer> layers = [];

        for (int index = 0; index < shapes.Count; index++)
        {
            layers.Add(new IconLayer(shapes[index], brushes[index]));
        }

        return new IconGeometry(layers, viewBox, GetCenter(viewBox));
    }

    private static IconGeometry Create(double viewBox, double offsetX, double offsetY, string path)
    {
        Geometry geometry = Geometry.Parse(path);

        if (offsetX != 0 || offsetY != 0)
        {
            geometry.Transform = new TranslateTransform(offsetX, offsetY);
        }

        return new IconGeometry([new IconLayer(geometry, null)], viewBox, GetCenter(viewBox));
    }

    private static Point GetCenter(double viewBox)
    {
        return new Point(viewBox / 2.0, viewBox / 2.0);
    }

    private static IImmutableBrush MakeSolidBrush(string color)
    {
        return new ImmutableSolidColorBrush(Color.Parse(color));
    }

    private static IImmutableBrush MakeLinearGradientBrush(Point start, Point end, ImmutableGradientStop[] stops)
    {
        return new ImmutableLinearGradientBrush(
            stops,
            startPoint: new RelativePoint(start, RelativeUnit.Absolute),
            endPoint: new RelativePoint(end, RelativeUnit.Absolute));
    }

    private static ImmutableGradientStop MakeGradientStop(string color, double offset)
    {
        return new ImmutableGradientStop(offset, Color.Parse(color));
    }
}
