using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Gallean.Common;
using Gallean.Engine.Contracts;

namespace Gallean.Assets;

public abstract record AssetAnimationKey : AssetKey, IParsable<AssetAnimationKey>
{
    public sealed record Water : AssetAnimationKey
    {
        internal const string Category = "water";

        public override string CanonicalId => Category;
    }

    public sealed record StackUnit(UnitDefinitionId DefinitionId, StackUnitPose Pose, int Direction) : AssetAnimationKey
    {
        internal const string Category = "stack-unit";

        public int Direction { get; } = Direction.Mod(DirectionCount);

        public override string CanonicalId => $"{Category}/{Token(DefinitionId)}/{Token(Pose)}/{Direction}";

        internal static StackUnit? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string unitId, string pose, string direction] => new StackUnit(
                    new UnitDefinitionId(unitId),
                    ParseEnum<StackUnitPose>(pose),
                    ParseInt(direction)),
                _ => null
            };
        }
    }

    public sealed record StackUnitShadow(UnitDefinitionId DefinitionId, StackUnitPose Pose, int Direction) : AssetAnimationKey
    {
        internal const string Category = "stack-unit-shadow";

        public int Direction { get; } = Direction.Mod(DirectionCount);

        public override string CanonicalId => $"{Category}/{Token(DefinitionId)}/{Token(Pose)}/{Direction}";

        internal static StackUnitShadow? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string unitId, string pose, string direction] => new StackUnitShadow(
                    new UnitDefinitionId(unitId),
                    ParseEnum<StackUnitPose>(pose),
                    ParseInt(direction)),
                _ => null
            };
        }
    }

    public sealed record StackUnitAura(UnitDefinitionId DefinitionId, StackUnitPose Pose, int Direction) : AssetAnimationKey
    {
        internal const string Category = "stack-unit-aura";

        public int Direction { get; } = Direction.Mod(DirectionCount);

        public override string CanonicalId => $"{Category}/{Token(DefinitionId)}/{Token(Pose)}/{Direction}";

        internal static StackUnitAura? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string unitId, string pose, string direction] => new StackUnitAura(
                    new UnitDefinitionId(unitId),
                    ParseEnum<StackUnitPose>(pose),
                    ParseInt(direction)),
                _ => null
            };
        }
    }

    public sealed record StackBoat(WorldRace Race, StackUnitPose Pose, int Direction) : AssetAnimationKey
    {
        internal const string Category = "stack-boat";

        public int Direction { get; } = Direction.Mod(DirectionCount);

        public override string CanonicalId => $"{Category}/{Token(Race)}/{Token(Pose)}/{Direction}";

        internal static StackBoat? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string race, string pose, string direction] => new StackBoat(
                    ParseEnum<WorldRace>(race),
                    ParseEnum<StackUnitPose>(pose),
                    ParseInt(direction)),
                _ => null
            };
        }
    }

    public sealed record StackBoatShadow(WorldRace Race, StackUnitPose Pose, int Direction) : AssetAnimationKey
    {
        internal const string Category = "stack-boat-shadow";

        public int Direction { get; } = Direction.Mod(DirectionCount);

        public override string CanonicalId => $"{Category}/{Token(Race)}/{Token(Pose)}/{Direction}";

        internal static StackBoatShadow? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string race, string pose, string direction] => new StackBoatShadow(
                    ParseEnum<WorldRace>(race),
                    ParseEnum<StackUnitPose>(pose),
                    ParseInt(direction)),
                _ => null
            };
        }
    }

    public sealed record StackDestroyEffect : AssetAnimationKey
    {
        internal const string Category = "stack-destroy-effect";

        public override string CanonicalId => Category;
    }

    public sealed record StackSummonDestroyEffect : AssetAnimationKey
    {
        internal const string Category = "stack-summon-destroy-effect";

        public override string CanonicalId => Category;
    }

    public sealed record StackCreateEffect : AssetAnimationKey
    {
        internal const string Category = "stack-create-effect";

        public override string CanonicalId => Category;
    }

    public sealed record StackOwnerChangeEffect : AssetAnimationKey
    {
        internal const string Category = "stack-owner-change-effect";

        public override string CanonicalId => Category;
    }

    public sealed record StackBattleEffect : AssetAnimationKey
    {
        internal const string Category = "stack-battle-effect";

        public override string CanonicalId => Category;
    }

    public sealed record MountainRemoveEffect(int Size) : AssetAnimationKey
    {
        internal const string Category = "mountain-remove-effect";

        public static readonly ImmutableArray<int> Sizes = [3, 5, 7];

        public int Size { get; } = SnapSize(Size);

        public override string CanonicalId => $"{Category}/{Size}";

        internal static MountainRemoveEffect? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string size] => new MountainRemoveEffect(ParseInt(size)),
                _ => null
            };
        }

        private static int SnapSize(int size)
        {
            int result = Sizes[0];

            foreach (int available in Sizes)
            {
                if (available <= size)
                {
                    result = available;
                }
            }

            return result;
        }
    }

    public sealed record BattleUnit(UnitDefinitionId DefinitionId, BattleUnitPose Pose, BattleSide Side) : AssetAnimationKey
    {
        internal const string Category = "battle-unit";

        public override string CanonicalId => $"{Category}/{Token(DefinitionId)}/{Token(Pose)}/{Token(Side)}";

        internal static BattleUnit? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string unitId, string pose, string side] => new BattleUnit(
                    new UnitDefinitionId(unitId),
                    ParseEnum<BattleUnitPose>(pose),
                    ParseEnum<BattleSide>(side)),
                _ => null
            };
        }
    }

    public sealed record BattleUnitShadow(UnitDefinitionId DefinitionId, BattleUnitPose Pose, BattleSide Side) : AssetAnimationKey
    {
        internal const string Category = "battle-unit-shadow";

        public override string CanonicalId => $"{Category}/{Token(DefinitionId)}/{Token(Pose)}/{Token(Side)}";

        internal static BattleUnitShadow? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string unitId, string pose, string side] => new BattleUnitShadow(
                    new UnitDefinitionId(unitId),
                    ParseEnum<BattleUnitPose>(pose),
                    ParseEnum<BattleSide>(side)),
                _ => null
            };
        }
    }

    public sealed record BattleUnitAura(UnitDefinitionId DefinitionId, BattleUnitPose Pose, BattleSide Side) : AssetAnimationKey
    {
        internal const string Category = "battle-unit-aura";

        public override string CanonicalId => $"{Category}/{Token(DefinitionId)}/{Token(Pose)}/{Token(Side)}";

        internal static BattleUnitAura? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string unitId, string pose, string side] => new BattleUnitAura(
                    new UnitDefinitionId(unitId),
                    ParseEnum<BattleUnitPose>(pose),
                    ParseEnum<BattleSide>(side)),
                _ => null
            };
        }
    }

    public abstract record BattleEffect : AssetAnimationKey
    {
        public sealed record Attack(
            UnitDefinitionId AttackerUnitId,
            AttackType Type,
            BattleLayer Layer,
            AttackSide Side,
            int Variant) : BattleEffect
        {
            internal const string Category = "battle-effect-attack";

            public const int VariantCount = 3;

            public int Variant { get; } = Variant.Mod(VariantCount);

            public override string CanonicalId => $"{Category}/{Token(AttackerUnitId)}/{Token(Type)}/{Token(Layer)}/{Token(Side)}/{Variant}";

            internal static Attack? Parse(IReadOnlyList<string> tokens)
            {
                return tokens switch
                {
                    [string attackerUnitId, string type, string layer, string side, string variant] => new Attack(
                        new UnitDefinitionId(attackerUnitId),
                        ParseEnum<AttackType>(type),
                        ParseEnum<BattleLayer>(layer),
                        ParseEnum<AttackSide>(side),
                        ParseInt(variant)),
                    _ => null
                };
            }
        }

        public sealed record Heal : BattleEffect
        {
            internal const string Category = "battle-effect-heal";

            public override string CanonicalId => Category;
        }

        public sealed record Blister : BattleEffect
        {
            internal const string Category = "battle-effect-blister";

            public override string CanonicalId => Category;
        }

        public sealed record Item(ItemType Type) : BattleEffect
        {
            internal const string Category = "battle-effect-item";

            public override string CanonicalId => $"{Category}/{Token(Type)}";

            internal static Item? Parse(IReadOnlyList<string> tokens)
            {
                return tokens switch
                {
                    [string type] => new Item(ParseEnum<ItemType>(type)),
                    _ => null
                };
            }
        }

        public sealed record Death(UnitDeathAnimation Animation, bool IsLarge) : BattleEffect
        {
            internal const string Category = "battle-effect-death";

            public override string CanonicalId => $"{Category}/{Token(Animation)}/{(IsLarge ? "large" : "small")}";

            internal static Death? Parse(IReadOnlyList<string> tokens)
            {
                return tokens switch
                {
                    [string animation, "large"] => new Death(ParseEnum<UnitDeathAnimation>(animation), IsLarge: true),
                    [string animation, "small"] => new Death(ParseEnum<UnitDeathAnimation>(animation), IsLarge: false),
                    _ => null
                };
            }
        }

        public sealed record LevelUp(WorldRace Race, bool IsLarge) : BattleEffect
        {
            internal const string Category = "battle-effect-level-up";

            public override string CanonicalId => $"{Category}/{Token(Race)}/{(IsLarge ? "large" : "small")}";

            internal static LevelUp? Parse(IReadOnlyList<string> tokens)
            {
                return tokens switch
                {
                    [string race, "large"] => new LevelUp(ParseEnum<WorldRace>(race), IsLarge: true),
                    [string race, "small"] => new LevelUp(ParseEnum<WorldRace>(race), IsLarge: false),
                    _ => null
                };
            }
        }

        public sealed record Status(StatusType Type, bool IsLarge) : BattleEffect
        {
            internal const string Category = "battle-effect-status";

            public override string CanonicalId => $"{Category}/{Token(Type)}/{(IsLarge ? "large" : "small")}";

            internal static Status? Parse(IReadOnlyList<string> tokens)
            {
                return tokens switch
                {
                    [string type, "large"] => new Status(ParseEnum<StatusType>(type), IsLarge: true),
                    [string type, "small"] => new Status(ParseEnum<StatusType>(type), IsLarge: false),
                    _ => null
                };
            }
        }

        public enum AttackType
        {
            Hit,
            Touch
        }

        public enum AttackSide
        {
            Attack,
            Defend,
            Both
        }

        public enum ItemType
        {
            Potion,
            Orb,
            Talisman
        }

        public enum StatusType
        {
            Poison,
            Frostbite,
            Blister,
            Revive,
            ReduceLevel,
            Transform,
            Doppelganger,
            Unsummon,
            RestoreLevel,
            Untransform,
            Undoppelganger
        }
    }

    public sealed record Crystal(WorldResourceType ResourceType) : AssetAnimationKey
    {
        internal const string Category = "crystal";

        public override string CanonicalId => $"{Category}/{Token(ResourceType)}";

        internal static Crystal? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string resourceType] => new Crystal(ParseEnum<WorldResourceType>(resourceType)),
                _ => null
            };
        }
    }

    public sealed record City(int Level) : AssetAnimationKey
    {
        internal const string Category = "city";

        public const int MinLevel = 1;
        public const int MaxLevel = 5;

        public int Level { get; } = Math.Clamp(Level, MinLevel, MaxLevel);

        public override string CanonicalId => $"{Category}/{Level}";

        internal static City? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string level] => new City(ParseInt(level)),
                _ => null
            };
        }
    }

    public sealed record CityShadow(int Level) : AssetAnimationKey
    {
        internal const string Category = "city-shadow";

        public int Level { get; } = Math.Clamp(Level, City.MinLevel, City.MaxLevel);

        public override string CanonicalId => $"{Category}/{Level}";

        internal static CityShadow? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string level] => new CityShadow(ParseInt(level)),
                _ => null
            };
        }
    }

    public sealed record Capital(WorldRace Race, bool IsCaptured) : AssetAnimationKey
    {
        internal const string Category = "capital";

        public override string CanonicalId => IsCaptured ? $"{Category}/captured/{Token(Race)}" : $"{Category}/{Token(Race)}";

        internal static Capital? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                ["captured", string race] => new Capital(ParseEnum<WorldRace>(race), IsCaptured: true),
                [string race] => new Capital(ParseEnum<WorldRace>(race), IsCaptured: false),
                _ => null
            };
        }
    }

    public sealed record BattleBackgroundOverlay(AssetSpriteKey.BattleLocation Location, int Variant, int Layer) : AssetAnimationKey
    {
        internal const string Category = "battle-background-overlay";

        public int Variant { get; } = Variant.Mod(AssetSpriteKey.BattleBackground.GetVariantCount(Location));

        public override string CanonicalId => Location switch
        {
            AssetSpriteKey.BattleLocation.Terrain terrain => $"{Category}/terrain/{Token(terrain.Value)}/{Variant}/{Layer}",
            AssetSpriteKey.BattleLocation.Boat => $"{Category}/boat/{Variant}/{Layer}",
            AssetSpriteKey.BattleLocation.City => $"{Category}/city/{Variant}/{Layer}",
            AssetSpriteKey.BattleLocation.Ruin => $"{Category}/ruin/{Variant}/{Layer}",
            AssetSpriteKey.BattleLocation.Capital capital => $"{Category}/capital/{Token(capital.Race)}/{Layer}",
            _ => throw new ArgumentOutOfRangeException(nameof(Location), Location, null)
        };

        internal static BattleBackgroundOverlay? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                ["terrain", string terrain, string variant, string layer] => new BattleBackgroundOverlay(
                    new AssetSpriteKey.BattleLocation.Terrain(ParseEnum<WorldMapTerrain>(terrain)), ParseInt(variant), ParseInt(layer)),
                ["boat", string variant, string layer] => new BattleBackgroundOverlay(
                    new AssetSpriteKey.BattleLocation.Boat(), ParseInt(variant), ParseInt(layer)),
                ["city", string variant, string layer] => new BattleBackgroundOverlay(
                    new AssetSpriteKey.BattleLocation.City(), ParseInt(variant), ParseInt(layer)),
                ["ruin", string variant, string layer] => new BattleBackgroundOverlay(
                    new AssetSpriteKey.BattleLocation.Ruin(), ParseInt(variant), ParseInt(layer)),
                ["capital", string race, string layer] => new BattleBackgroundOverlay(
                    new AssetSpriteKey.BattleLocation.Capital(ParseEnum<WorldRace>(race)), 0, ParseInt(layer)),
                _ => null
            };
        }
    }

    public sealed record CapitalBackgroundOverlay(WorldRace Race, int Variant) : AssetAnimationKey
    {
        internal const string Category = "capital-background-overlay";

        public const int VariantCount = 2;

        public int Variant { get; } = Variant.Mod(VariantCount);

        public override string CanonicalId => $"{Category}/{Token(Race)}/{Variant}";

        internal static CapitalBackgroundOverlay? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string race, string variant] => new CapitalBackgroundOverlay(
                    ParseEnum<WorldRace>(race),
                    ParseInt(variant)),
                _ => null
            };
        }
    }

    public sealed record MainMenuBackgroundOverlay(int Variant) : AssetAnimationKey
    {
        internal const string Category = "main-menu-background-overlay";

        public const int VariantCount = 2;

        public int Variant { get; } = Variant.Mod(VariantCount);

        public override string CanonicalId => $"{Category}/{Variant}";

        internal static MainMenuBackgroundOverlay? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string variant] => new MainMenuBackgroundOverlay(ParseInt(variant)),
                _ => null
            };
        }
    }

    public sealed record RaceSelectionBackgroundOverlay(WorldRace Race, int Variant) : AssetAnimationKey
    {
        internal const string Category = "race-selection-background-overlay";

        public const int VariantCount = 2;

        public int Variant { get; } = Variant.Mod(VariantCount);

        public override string CanonicalId => $"{Category}/{Token(Race)}/{Variant}";

        internal static RaceSelectionBackgroundOverlay? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string race, string variant] => new RaceSelectionBackgroundOverlay(
                    ParseEnum<WorldRace>(race),
                    ParseInt(variant)),
                _ => null
            };
        }
    }

    public sealed record CapitalBuilding(WorldPlayerBuildingDefinitionId BuildingId) : AssetAnimationKey
    {
        internal const string Category = "capital-building";

        public override string CanonicalId => $"{Category}/{Token(BuildingId)}";

        internal static CapitalBuilding? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string buildingId] => new CapitalBuilding(new WorldPlayerBuildingDefinitionId(buildingId)),
                _ => null
            };
        }
    }

    public sealed record Ruin(bool IsLooted, int Variant) : AssetAnimationKey
    {
        internal const string Category = "ruin";

        public const int VariantCount = 11;

        public int Variant { get; } = Variant.Mod(VariantCount);

        public override string CanonicalId => IsLooted ? $"{Category}/looted/{Variant}" : $"{Category}/intact/{Variant}";

        internal static Ruin? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                ["looted", string variant] => new Ruin(IsLooted: true, ParseInt(variant)),
                ["intact", string variant] => new Ruin(IsLooted: false, ParseInt(variant)),
                _ => null
            };
        }
    }

    public sealed record Landmark(WorldLandmarkDefinitionId LandmarkId) : AssetAnimationKey
    {
        internal const string Category = "landmark";

        public override string CanonicalId => $"{Category}/{Token(LandmarkId)}";

        internal static Landmark? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string landmarkId] => new Landmark(new WorldLandmarkDefinitionId(landmarkId)),
                _ => null
            };
        }
    }

    public sealed record WaterLandmark(WorldLandmarkDefinitionId LandmarkId) : AssetAnimationKey
    {
        internal const string Category = "water-landmark";

        public override string CanonicalId => $"{Category}/{Token(LandmarkId)}";

        internal static WaterLandmark? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string landmarkId] => new WaterLandmark(new WorldLandmarkDefinitionId(landmarkId)),
                _ => null
            };
        }
    }

    public sealed record Site(SiteType Type, int Variant) : AssetAnimationKey
    {
        internal const string Category = "site";

        public int Variant { get; } = Variant.Mod(GetVariantCount(Type));

        public override string CanonicalId => $"{Category}/{Token(Type)}/{Variant}";

        internal static Site? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string type, string variant] => new Site(ParseEnum<SiteType>(type), ParseInt(variant)),
                _ => null
            };
        }

        public static int GetVariantCount(SiteType type)
        {
            return type switch
            {
                SiteType.Mage => 4,
                SiteType.Merchant => 8,
                SiteType.Mercenary => 5,
                SiteType.Trainer => 4
            };
        }
    }

    public sealed record SiteShadow(SiteType Type, int Variant) : AssetAnimationKey
    {
        internal const string Category = "site-shadow";

        public int Variant { get; } = Variant.Mod(Site.GetVariantCount(Type));

        public override string CanonicalId => $"{Category}/{Token(Type)}/{Variant}";

        internal static SiteShadow? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string type, string variant] => new SiteShadow(ParseEnum<SiteType>(type), ParseInt(variant)),
                _ => null
            };
        }
    }

    public sealed record SiteBanner(SiteType Type) : AssetAnimationKey
    {
        internal const string Category = "site-banner";

        public override string CanonicalId => $"{Category}/{Token(Type)}";

        internal static SiteBanner? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string type] => new SiteBanner(ParseEnum<SiteType>(type)),
                _ => null
            };
        }
    }

    public sealed record Cloud(int Variant) : AssetAnimationKey
    {
        internal const string Category = "cloud";

        public const int VariantCount = 3;

        public int Variant { get; } = Variant.Mod(VariantCount);

        public override string CanonicalId => $"{Category}/{Variant}";

        internal static Cloud? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string variant] => new Cloud(ParseInt(variant)),
                _ => null
            };
        }
    }

    public sealed record Cursor(CursorType Type) : AssetAnimationKey
    {
        internal const string Category = "cursor";

        public override string CanonicalId => $"{Category}/{Token(Type)}";

        public Cursor Arrow => new(GetArrow(Type));
        public Cursor? Overlay => HasOverlay(Type) ? this : null;

        private static CursorType GetArrow(CursorType type)
        {
            return type switch
            {
                CursorType.Attack => CursorType.Default,
                CursorType.EnterCity => CursorType.Default,
                CursorType.Move => CursorType.Default,
                CursorType.SelectCity => CursorType.Default,
                CursorType.ViewCity => CursorType.Default,
                CursorType.ManageStack => CursorType.Default,
                CursorType.Visit => CursorType.Default,
                CursorType.CreateRod => CursorType.Default,
                CursorType.CreateRodInvalid => CursorType.Default,
                CursorType.DestroyRod => CursorType.Default,
                _ => type
            };
        }

        private static bool HasOverlay(CursorType type)
        {
            return type switch
            {
                CursorType.EnterCity => true,
                CursorType.SelectCity => true,
                CursorType.ViewCity => true,
                CursorType.ManageStack => true,
                CursorType.Visit => true,
                CursorType.CreateRod => true,
                CursorType.CreateRodInvalid => true,
                CursorType.DestroyRod => true,
                _ => false
            };
        }

        internal static Cursor? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string type] => new Cursor(ParseEnum<CursorType>(type)),
                _ => null
            };
        }
    }

    public sealed record WorldSpellEffect(
        WorldSpellDefinitionId SpellId,
        WorldSpellEffectLayer Layer,
        int Sublayer) : AssetAnimationKey
    {
        internal const string Category = "world-spell-effect";

        public const int SublayerCount = 2;

        public int Sublayer { get; } = Sublayer.Mod(SublayerCount);

        public override string CanonicalId => $"{Category}/{Token(SpellId)}/{Token(Layer)}/{Sublayer}";

        internal static WorldSpellEffect? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string spellId, string layer, string sublayer] => new WorldSpellEffect(
                    new WorldSpellDefinitionId(spellId),
                    ParseEnum<WorldSpellEffectLayer>(layer),
                    ParseInt(sublayer)),
                _ => null
            };
        }
    }

    public enum BattleUnitPose
    {
        Idle,
        Attack,
        Hit
    }

    public enum BattleLayer
    {
        Shadow,
        Body,
        Aura
    }

    public enum CursorType
    {
        Default,
        Wait,
        Alliance,
        Attack,
        Negotiate,
        EnterCity,
        Move,
        SelectUnit,
        SelectCity,
        ViewCity,
        ManageStack,
        UseScroll,
        UseScrollInvalid,
        CastSpell,
        CastSpellInvalid,
        TakeBag,
        Exchange,
        Visit,
        DropBag,
        CreateRod,
        CreateRodInvalid,
        DestroyRod,
        DragInvalid,
        Dismiss,
        DismissInvalid,
        Heal,
        HealInvalid,
        Revive,
        ReviveInvalid,
        DynamicLevel,
        DynamicLevelInvalid,
        BattleDefault,
        BattleAttack,
        BattleAttackInvalid,
        BattleHeal,
        BattleHealInvalid,
        BattleTransform,
        BattleTransformInvalid
    }

    public enum SiteType
    {
        Mage,
        Merchant,
        Mercenary,
        Trainer
    }

    public enum StackUnitPose
    {
        Move,
        Stop
    }

    public enum WorldSpellEffectLayer
    {
        Ground,
        Under,
        Front,
        Over,
        Top
    }

    public const int DirectionCount = 8;

    private static readonly FrozenDictionary<string, Func<IReadOnlyList<string>, AssetAnimationKey?>> Parsers =
        new Dictionary<string, Func<IReadOnlyList<string>, AssetAnimationKey?>>(StringComparer.Ordinal)
        {
            [Water.Category] = _ => new Water(),
            [StackUnit.Category] = StackUnit.Parse,
            [StackUnitShadow.Category] = StackUnitShadow.Parse,
            [StackUnitAura.Category] = StackUnitAura.Parse,
            [StackBoat.Category] = StackBoat.Parse,
            [StackBoatShadow.Category] = StackBoatShadow.Parse,
            [StackDestroyEffect.Category] = _ => new StackDestroyEffect(),
            [StackSummonDestroyEffect.Category] = _ => new StackSummonDestroyEffect(),
            [StackCreateEffect.Category] = _ => new StackCreateEffect(),
            [StackOwnerChangeEffect.Category] = _ => new StackOwnerChangeEffect(),
            [StackBattleEffect.Category] = _ => new StackBattleEffect(),
            [MountainRemoveEffect.Category] = MountainRemoveEffect.Parse,
            [BattleUnit.Category] = BattleUnit.Parse,
            [BattleUnitShadow.Category] = BattleUnitShadow.Parse,
            [BattleUnitAura.Category] = BattleUnitAura.Parse,
            [BattleEffect.Attack.Category] = BattleEffect.Attack.Parse,
            [BattleEffect.Heal.Category] = _ => new BattleEffect.Heal(),
            [BattleEffect.Blister.Category] = _ => new BattleEffect.Blister(),
            [BattleEffect.Item.Category] = BattleEffect.Item.Parse,
            [BattleEffect.Death.Category] = BattleEffect.Death.Parse,
            [BattleEffect.LevelUp.Category] = BattleEffect.LevelUp.Parse,
            [BattleEffect.Status.Category] = BattleEffect.Status.Parse,
            [Crystal.Category] = Crystal.Parse,
            [City.Category] = City.Parse,
            [CityShadow.Category] = CityShadow.Parse,
            [Capital.Category] = Capital.Parse,
            [BattleBackgroundOverlay.Category] = BattleBackgroundOverlay.Parse,
            [CapitalBackgroundOverlay.Category] = CapitalBackgroundOverlay.Parse,
            [MainMenuBackgroundOverlay.Category] = MainMenuBackgroundOverlay.Parse,
            [RaceSelectionBackgroundOverlay.Category] = RaceSelectionBackgroundOverlay.Parse,
            [CapitalBuilding.Category] = CapitalBuilding.Parse,
            [Ruin.Category] = Ruin.Parse,
            [Landmark.Category] = Landmark.Parse,
            [WaterLandmark.Category] = WaterLandmark.Parse,
            [Site.Category] = Site.Parse,
            [SiteShadow.Category] = SiteShadow.Parse,
            [SiteBanner.Category] = SiteBanner.Parse,
            [Cloud.Category] = Cloud.Parse,
            [Cursor.Category] = Cursor.Parse,
            [WorldSpellEffect.Category] = WorldSpellEffect.Parse
        }.ToFrozenDictionary(StringComparer.Ordinal);

    internal static IReadOnlyCollection<string> Categories => Parsers.Keys;

    public new static AssetAnimationKey Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out AssetAnimationKey? key))
        {
            return key;
        }
        else
        {
            throw new FormatException($"The '{s}' asset key is not valid.");
        }
    }

    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out AssetAnimationKey result)
    {
        AssetAnimationKey? key = ParseKey(s);

        if (key is not null && key.CanonicalId == s)
        {
            result = key;

            return true;
        }
        else
        {
            result = null;

            return false;
        }
    }

    private static AssetAnimationKey? ParseKey(string? id)
    {
        if (id is null)
        {
            return null;
        }

        string[] tokens = id.Split('/');

        if (Parsers.TryGetValue(tokens[0], out Func<IReadOnlyList<string>, AssetAnimationKey?>? parse))
        {
            return parse(tokens[1..]);
        }
        else
        {
            return null;
        }
    }
}
