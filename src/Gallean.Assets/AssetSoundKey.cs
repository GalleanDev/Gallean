using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Gallean.Common;
using Gallean.Engine.Contracts;

namespace Gallean.Assets;

public abstract record AssetSoundKey : AssetKey, IParsable<AssetSoundKey>
{
    public sealed record MenuMusic : AssetSoundKey
    {
        internal const string Category = "menu-music";

        public override string CanonicalId => Category;
    }

    public sealed record WorldMusic(int Variant) : AssetSoundKey
    {
        internal const string Category = "world-music";

        public const int VariantCount = 11;

        public int Variant { get; } = Variant.Mod(VariantCount);

        public override string CanonicalId => $"{Category}/{Variant}";

        internal static WorldMusic? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string variant] => new WorldMusic(ParseInt(variant)),
                _ => null
            };
        }
    }

    public sealed record AmbientMusic(int Variant) : AssetSoundKey
    {
        internal const string Category = "ambient-music";

        public const int VariantCount = 11;

        public int Variant { get; } = Variant.Mod(VariantCount);

        public override string CanonicalId => $"{Category}/{Variant}";

        internal static AmbientMusic? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string variant] => new AmbientMusic(ParseInt(variant)),
                _ => null
            };
        }
    }

    public sealed record CapitalMusic(WorldRace Race, int Variant) : AssetSoundKey
    {
        internal const string Category = "capital-music";

        public const int VariantCount = 3;

        public int Variant { get; } = Variant.Mod(VariantCount);

        public override string CanonicalId => $"{Category}/{Token(Race)}/{Variant}";

        internal static CapitalMusic? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string race, string variant] => new CapitalMusic(ParseEnum<WorldRace>(race), ParseInt(variant)),
                _ => null
            };
        }
    }

    public sealed record BattleMusic(int Variant) : AssetSoundKey
    {
        internal const string Category = "battle-music";

        public const int VariantCount = 4;

        public int Variant { get; } = Variant.Mod(VariantCount);

        public override string CanonicalId => $"{Category}/{Variant}";

        internal static BattleMusic? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string variant] => new BattleMusic(ParseInt(variant)),
                _ => null
            };
        }
    }

    public sealed record BriefingMusic : AssetSoundKey
    {
        internal const string Category = "briefing-music";

        public override string CanonicalId => Category;
    }

    public sealed record VictoryMusic(ScenarioId ScenarioId) : AssetSoundKey
    {
        internal const string Category = "victory-music";

        public override string CanonicalId => $"{Category}/{Token(ScenarioId)}";

        internal static VictoryMusic? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string scenarioId] => new VictoryMusic(new ScenarioId(scenarioId)),
                _ => null
            };
        }
    }

    public sealed record BriefingVoice(ScenarioId ScenarioId) : AssetSoundKey
    {
        internal const string Category = "briefing-voice";

        public override string CanonicalId => $"{Category}/{Token(ScenarioId)}";

        internal static BriefingVoice? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string scenarioId] => new BriefingVoice(new ScenarioId(scenarioId)),
                _ => null
            };
        }
    }

    public sealed record InterfaceOpen : AssetSoundKey
    {
        internal const string Category = "interface-open";

        public override string CanonicalId => Category;
    }

    public sealed record Beep : AssetSoundKey
    {
        internal const string Category = "beep";

        public override string CanonicalId => Category;
    }

    public sealed record BookOpen : AssetSoundKey
    {
        internal const string Category = "book-open";

        public override string CanonicalId => Category;
    }

    public sealed record BookClose : AssetSoundKey
    {
        internal const string Category = "book-close";

        public override string CanonicalId => Category;
    }

    public sealed record TurnStart : AssetSoundKey
    {
        internal const string Category = "turn-start";

        public override string CanonicalId => Category;
    }

    public sealed record WorldBattleStart : AssetSoundKey
    {
        internal const string Category = "world-battle-start";

        public override string CanonicalId => Category;
    }

    public sealed record Occupy : AssetSoundKey
    {
        internal const string Category = "occupy";

        public override string CanonicalId => Category;
    }

    public sealed record RuinEnter : AssetSoundKey
    {
        internal const string Category = "ruin-enter";

        public override string CanonicalId => Category;
    }

    public sealed record RuinLoot : AssetSoundKey
    {
        internal const string Category = "ruin-loot";

        public override string CanonicalId => Category;
    }

    public sealed record BuildingConstruct : AssetSoundKey
    {
        internal const string Category = "building-construct";

        public override string CanonicalId => Category;
    }

    public sealed record CityGrow : AssetSoundKey
    {
        internal const string Category = "city-grow";

        public override string CanonicalId => Category;
    }

    public sealed record CityEnter : AssetSoundKey
    {
        internal const string Category = "city-enter";

        public override string CanonicalId => Category;
    }

    public sealed record CityExit : AssetSoundKey
    {
        internal const string Category = "city-exit";

        public override string CanonicalId => Category;
    }

    public sealed record CityRiotEnd : AssetSoundKey
    {
        internal const string Category = "city-riot-end";

        public override string CanonicalId => Category;
    }

    public sealed record BagPickUp : AssetSoundKey
    {
        internal const string Category = "bag-pick-up";

        public override string CanonicalId => Category;
    }

    public sealed record UnitPurchase : AssetSoundKey
    {
        internal const string Category = "unit-purchase";

        public override string CanonicalId => Category;
    }

    public sealed record SitePurchase : AssetSoundKey
    {
        internal const string Category = "site-purchase";

        public override string CanonicalId => Category;
    }

    public sealed record SiteEnter : AssetSoundKey
    {
        internal const string Category = "site-enter";

        public override string CanonicalId => Category;
    }

    public sealed record ItemUse : AssetSoundKey
    {
        internal const string Category = "item-use";

        public override string CanonicalId => Category;
    }

    public sealed record SpellResearch : AssetSoundKey
    {
        internal const string Category = "spell-research";

        public override string CanonicalId => Category;
    }

    public sealed record ThiefAction : AssetSoundKey
    {
        internal const string Category = "thief-action";

        public override string CanonicalId => Category;
    }

    public sealed record BattleUnitAttack(UnitDefinitionId DefinitionId, int Variant) : AssetSoundKey
    {
        internal const string Category = "battle-unit-attack";

        public const int VariantCount = 6;

        public int Variant { get; } = Variant.Mod(VariantCount);

        public override string CanonicalId => $"{Category}/{Token(DefinitionId)}/{Variant}";

        internal static BattleUnitAttack? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string unitId, string variant] => new BattleUnitAttack(
                    new UnitDefinitionId(unitId),
                    ParseInt(variant)),
                _ => null
            };
        }
    }

    public sealed record BattleUnitHit(UnitDefinitionId DefinitionId, int Variant) : AssetSoundKey
    {
        internal const string Category = "battle-unit-hit";

        public const int VariantCount = 5;

        public int Variant { get; } = Variant.Mod(VariantCount);

        public override string CanonicalId => $"{Category}/{Token(DefinitionId)}/{Variant}";

        internal static BattleUnitHit? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string unitId, string variant] => new BattleUnitHit(
                    new UnitDefinitionId(unitId),
                    ParseInt(variant)),
                _ => null
            };
        }
    }

    public sealed record BattleUnitTouch(UnitDefinitionId DefinitionId, int Variant) : AssetSoundKey
    {
        internal const string Category = "battle-unit-touch";

        public const int VariantCount = 5;

        public int Variant { get; } = Variant.Mod(VariantCount);

        public override string CanonicalId => $"{Category}/{Token(DefinitionId)}/{Variant}";

        internal static BattleUnitTouch? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string unitId, string variant] => new BattleUnitTouch(
                    new UnitDefinitionId(unitId),
                    ParseInt(variant)),
                _ => null
            };
        }
    }

    public sealed record BattleUnitDamaged(UnitDefinitionId DefinitionId, int Variant) : AssetSoundKey
    {
        internal const string Category = "battle-unit-damaged";

        public const int VariantCount = 4;

        public int Variant { get; } = Variant.Mod(VariantCount);

        public override string CanonicalId => $"{Category}/{Token(DefinitionId)}/{Variant}";

        internal static BattleUnitDamaged? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string unitId, string variant] => new BattleUnitDamaged(
                    new UnitDefinitionId(unitId),
                    ParseInt(variant)),
                _ => null
            };
        }
    }

    public abstract record BattleEffect : AssetSoundKey
    {
        public sealed record Heal : BattleEffect
        {
            internal const string Category = "battle-effect-sound-heal";

            public override string CanonicalId => Category;
        }

        public sealed record Death : BattleEffect
        {
            internal const string Category = "battle-effect-sound-death";

            public override string CanonicalId => Category;
        }

        public sealed record Status(StatusType Type) : BattleEffect
        {
            internal const string Category = "battle-effect-sound-status";

            public override string CanonicalId => $"{Category}/{Token(Type)}";

            internal static Status? Parse(IReadOnlyList<string> tokens)
            {
                return tokens switch
                {
                    [string type] => new Status(ParseEnum<StatusType>(type)),
                    _ => null
                };
            }
        }

        public sealed record Potion(PotionType Type) : BattleEffect
        {
            internal const string Category = "battle-effect-sound-potion";

            public override string CanonicalId => $"{Category}/{Token(Type)}";

            internal static Potion? Parse(IReadOnlyList<string> tokens)
            {
                return tokens switch
                {
                    [string type] => new Potion(ParseEnum<PotionType>(type)),
                    _ => null
                };
            }
        }

        public enum StatusType
        {
            Poison,
            Frostbite,
            Blister,
            ShatterArmor,
            Transform,
            Unsummon
        }

        public enum PotionType
        {
            Boost,
            Heal,
            Revive
        }
    }

    public sealed record StackUnitMove(UnitDefinitionId DefinitionId, int Variant) : AssetSoundKey
    {
        internal const string Category = "stack-unit-move";

        public const int VariantCount = 4;

        public int Variant { get; } = Variant.Mod(VariantCount);

        public override string CanonicalId => $"{Category}/{Token(DefinitionId)}/{Variant}";

        internal static StackUnitMove? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string unitId, string variant] => new StackUnitMove(
                    new UnitDefinitionId(unitId),
                    ParseInt(variant)),
                _ => null
            };
        }
    }

    public sealed record StackBoatMove : AssetSoundKey
    {
        internal const string Category = "stack-boat-move";

        public override string CanonicalId => Category;
    }

    public sealed record StackUnitResponse(UnitDefinitionId DefinitionId, int Variant) : AssetSoundKey
    {
        internal const string Category = "stack-unit-response";

        public const int VariantCount = 4;

        public int Variant { get; } = Variant.Mod(VariantCount);

        public override string CanonicalId => $"{Category}/{Token(DefinitionId)}/{Variant}";

        internal static StackUnitResponse? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string unitId, string variant] => new StackUnitResponse(
                    new UnitDefinitionId(unitId),
                    ParseInt(variant)),
                _ => null
            };
        }
    }

    public sealed record StackUnitAttack(UnitDefinitionId DefinitionId) : AssetSoundKey
    {
        internal const string Category = "stack-unit-attack";

        public override string CanonicalId => $"{Category}/{Token(DefinitionId)}";

        internal static StackUnitAttack? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string unitId] => new StackUnitAttack(new UnitDefinitionId(unitId)),
                _ => null
            };
        }
    }

    public sealed record StackUnitIdle(UnitDefinitionId DefinitionId, int Variant) : AssetSoundKey
    {
        internal const string Category = "stack-unit-idle";

        public const int VariantCount = 2;

        public int Variant { get; } = Variant.Mod(VariantCount);

        public override string CanonicalId => $"{Category}/{Token(DefinitionId)}/{Variant}";

        internal static StackUnitIdle? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string unitId, string variant] => new StackUnitIdle(
                    new UnitDefinitionId(unitId),
                    ParseInt(variant)),
                _ => null
            };
        }
    }

    public sealed record StackUnitDeath(UnitDefinitionId DefinitionId) : AssetSoundKey
    {
        internal const string Category = "stack-unit-death";

        public override string CanonicalId => $"{Category}/{Token(DefinitionId)}";

        internal static StackUnitDeath? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string unitId] => new StackUnitDeath(new UnitDefinitionId(unitId)),
                _ => null
            };
        }
    }

    public sealed record StackDestroy : AssetSoundKey
    {
        internal const string Category = "stack-destroy";

        public override string CanonicalId => Category;
    }

    public sealed record StackSummonDestroy : AssetSoundKey
    {
        internal const string Category = "stack-summon-destroy";

        public override string CanonicalId => Category;
    }

    public sealed record StackCreate : AssetSoundKey
    {
        internal const string Category = "stack-create";

        public override string CanonicalId => Category;
    }

    public sealed record WorldSpellCast(WorldSpellDefinitionId SpellId) : AssetSoundKey
    {
        internal const string Category = "world-spell-cast";

        public override string CanonicalId => $"{Category}/{Token(SpellId)}";

        internal static WorldSpellCast? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string spellId] => new WorldSpellCast(new WorldSpellDefinitionId(spellId)),
                _ => null
            };
        }
    }

    public sealed record Ambient(AmbientSoundType Type) : AssetSoundKey
    {
        internal const string Category = "ambient";

        public override string CanonicalId => $"{Category}/{Token(Type)}";

        internal static Ambient? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string type] => new Ambient(ParseEnum<AmbientSoundType>(type)),
                _ => null
            };
        }
    }

    public sealed record AmbientCapital(WorldRace Race) : AssetSoundKey
    {
        internal const string Category = "ambient-capital";

        public override string CanonicalId => $"{Category}/{Token(Race)}";

        internal static AmbientCapital? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string race] => new AmbientCapital(ParseEnum<WorldRace>(race)),
                _ => null
            };
        }
    }

    public sealed record AmbientRuin(int Variant) : AssetSoundKey
    {
        internal const string Category = "ambient-ruin";

        public const int VariantCount = 4;

        public int Variant { get; } = Variant.Mod(VariantCount);

        public override string CanonicalId => $"{Category}/{Variant}";

        internal static AmbientRuin? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string variant] => new AmbientRuin(ParseInt(variant)),
                _ => null
            };
        }
    }

    public enum AmbientSoundType
    {
        City,
        Waterfall,
        Cemetery,
        Lava,
        Torch,
        Whirlpool,
        CityInFlames,
        GateClosed,
        GateOpen,
        GoldMine,
        MagicTower,
        Windmill
    }

    private static readonly FrozenDictionary<string, Func<IReadOnlyList<string>, AssetSoundKey?>> Parsers =
        new Dictionary<string, Func<IReadOnlyList<string>, AssetSoundKey?>>(StringComparer.Ordinal)
        {
            [MenuMusic.Category] = _ => new MenuMusic(),
            [WorldMusic.Category] = WorldMusic.Parse,
            [AmbientMusic.Category] = AmbientMusic.Parse,
            [CapitalMusic.Category] = CapitalMusic.Parse,
            [BattleMusic.Category] = BattleMusic.Parse,
            [BriefingMusic.Category] = _ => new BriefingMusic(),
            [VictoryMusic.Category] = VictoryMusic.Parse,
            [BriefingVoice.Category] = BriefingVoice.Parse,
            [InterfaceOpen.Category] = _ => new InterfaceOpen(),
            [Beep.Category] = _ => new Beep(),
            [BookOpen.Category] = _ => new BookOpen(),
            [BookClose.Category] = _ => new BookClose(),
            [TurnStart.Category] = _ => new TurnStart(),
            [WorldBattleStart.Category] = _ => new WorldBattleStart(),
            [Occupy.Category] = _ => new Occupy(),
            [RuinEnter.Category] = _ => new RuinEnter(),
            [CityEnter.Category] = _ => new CityEnter(),
            [CityExit.Category] = _ => new CityExit(),
            [RuinLoot.Category] = _ => new RuinLoot(),
            [BuildingConstruct.Category] = _ => new BuildingConstruct(),
            [CityGrow.Category] = _ => new CityGrow(),
            [CityRiotEnd.Category] = _ => new CityRiotEnd(),
            [BagPickUp.Category] = _ => new BagPickUp(),
            [UnitPurchase.Category] = _ => new UnitPurchase(),
            [SitePurchase.Category] = _ => new SitePurchase(),
            [SiteEnter.Category] = _ => new SiteEnter(),
            [ItemUse.Category] = _ => new ItemUse(),
            [SpellResearch.Category] = _ => new SpellResearch(),
            [ThiefAction.Category] = _ => new ThiefAction(),
            [BattleUnitAttack.Category] = BattleUnitAttack.Parse,
            [BattleUnitHit.Category] = BattleUnitHit.Parse,
            [BattleUnitTouch.Category] = BattleUnitTouch.Parse,
            [BattleUnitDamaged.Category] = BattleUnitDamaged.Parse,
            [BattleEffect.Heal.Category] = _ => new BattleEffect.Heal(),
            [BattleEffect.Death.Category] = _ => new BattleEffect.Death(),
            [BattleEffect.Status.Category] = BattleEffect.Status.Parse,
            [BattleEffect.Potion.Category] = BattleEffect.Potion.Parse,
            [StackUnitMove.Category] = StackUnitMove.Parse,
            [StackBoatMove.Category] = _ => new StackBoatMove(),
            [StackUnitResponse.Category] = StackUnitResponse.Parse,
            [StackUnitAttack.Category] = StackUnitAttack.Parse,
            [StackUnitIdle.Category] = StackUnitIdle.Parse,
            [StackUnitDeath.Category] = StackUnitDeath.Parse,
            [StackDestroy.Category] = _ => new StackDestroy(),
            [StackSummonDestroy.Category] = _ => new StackSummonDestroy(),
            [StackCreate.Category] = _ => new StackCreate(),
            [WorldSpellCast.Category] = WorldSpellCast.Parse,
            [Ambient.Category] = Ambient.Parse,
            [AmbientCapital.Category] = AmbientCapital.Parse,
            [AmbientRuin.Category] = AmbientRuin.Parse
        }.ToFrozenDictionary(StringComparer.Ordinal);

    internal static IReadOnlyCollection<string> Categories => Parsers.Keys;

    public new static AssetSoundKey Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out AssetSoundKey? key))
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
        [MaybeNullWhen(false)] out AssetSoundKey result)
    {
        AssetSoundKey? key = ParseKey(s);

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

    private static AssetSoundKey? ParseKey(string? id)
    {
        if (id is null)
        {
            return null;
        }

        string[] tokens = id.Split('/');

        if (Parsers.TryGetValue(tokens[0], out Func<IReadOnlyList<string>, AssetSoundKey?>? parse))
        {
            return parse(tokens[1..]);
        }
        else
        {
            return null;
        }
    }
}
