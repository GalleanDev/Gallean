using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Gallean.Common;
using Gallean.Engine.Contracts;

namespace Gallean.Assets;

public abstract record AssetSpriteKey : AssetKey, IParsable<AssetSpriteKey>
{
    public sealed record Ground(WorldMapTerrain Terrain) : AssetSpriteKey
    {
        internal const string Category = "ground";

        public override string CanonicalId => $"{Category}/{Token(Terrain)}";

        internal static Ground? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string terrain] => new Ground(ParseEnum<WorldMapTerrain>(terrain)),
                _ => null
            };
        }
    }

    public abstract record GroundBorderShape
    {
        public sealed record Edges(GroundBorderEdges Value) : GroundBorderShape;

        public sealed record Corners(GroundBorderCorners Value) : GroundBorderShape;
    }

    public sealed record GroundBorder(GroundBorderSet Set, GroundBorderShape Shape, int Variant) : AssetSpriteKey
    {
        internal const string Category = "ground-border";

        public int Variant { get; } = Variant.Mod(GetVariantCount(Set, Shape));

        public override string CanonicalId => $"{Category}/{SetToken()}/{ShapeToken()}/{Variant}";

        internal static GroundBorder? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string set, "edge", string edges, string variant] => new GroundBorder(
                    ParseEnum<GroundBorderSet>(set), new GroundBorderShape.Edges(ParseEdges(edges)), ParseInt(variant)),
                [string set, "corner", string corners, string variant] => new GroundBorder(
                    ParseEnum<GroundBorderSet>(set), new GroundBorderShape.Corners(ParseCorners(corners)), ParseInt(variant)),
                _ => null
            };
        }

        private static GroundBorderEdges ParseEdges(string token)
        {
            GroundBorderEdges edges = GroundBorderEdges.None;

            foreach (char ch in token)
            {
                edges |= ch switch
                {
                    'n' => GroundBorderEdges.North,
                    'e' => GroundBorderEdges.East,
                    's' => GroundBorderEdges.South,
                    'w' => GroundBorderEdges.West,
                    _ => GroundBorderEdges.None
                };
            }

            return edges;
        }

        private static GroundBorderCorners ParseCorners(string token)
        {
            GroundBorderCorners corners = GroundBorderCorners.None;

            for (int index = 0; index + 1 < token.Length; index += 2)
            {
                corners |= token.Substring(index, 2) switch
                {
                    "nw" => GroundBorderCorners.NorthWest,
                    "ne" => GroundBorderCorners.NorthEast,
                    "se" => GroundBorderCorners.SouthEast,
                    "sw" => GroundBorderCorners.SouthWest,
                    _ => GroundBorderCorners.None
                };
            }

            return corners;
        }

        private string SetToken()
        {
            return Set switch
            {
                GroundBorderSet.Mask => "mask",
                GroundBorderSet.Coast => "coast"
            };
        }

        private string ShapeToken()
        {
            return Shape switch
            {
                GroundBorderShape.Edges edges => $"edge/{EdgesToken(edges.Value)}",
                GroundBorderShape.Corners corners => $"corner/{CornersToken(corners.Value)}",
                _ => throw new ArgumentOutOfRangeException(nameof(Shape), Shape, null)
            };
        }

        private static string EdgesToken(GroundBorderEdges edges)
        {
            string token = "";

            if (edges.HasFlag(GroundBorderEdges.North))
            {
                token += "n";
            }

            if (edges.HasFlag(GroundBorderEdges.East))
            {
                token += "e";
            }

            if (edges.HasFlag(GroundBorderEdges.South))
            {
                token += "s";
            }

            if (edges.HasFlag(GroundBorderEdges.West))
            {
                token += "w";
            }

            return token;
        }

        private static string CornersToken(GroundBorderCorners corners)
        {
            string token = "";

            if (corners.HasFlag(GroundBorderCorners.NorthWest))
            {
                token += "nw";
            }

            if (corners.HasFlag(GroundBorderCorners.NorthEast))
            {
                token += "ne";
            }

            if (corners.HasFlag(GroundBorderCorners.SouthEast))
            {
                token += "se";
            }

            if (corners.HasFlag(GroundBorderCorners.SouthWest))
            {
                token += "sw";
            }

            return token;
        }

        public static int GetVariantCount(GroundBorderSet set, GroundBorderShape shape)
        {
            if (shape is GroundBorderShape.Edges edges)
            {
                if (set == GroundBorderSet.Coast)
                {
                    return CoastEdgeVariantCount(edges.Value);
                }
                else
                {
                    return MaskEdgeVariantCount(edges.Value);
                }
            }
            else
            {
                return 1;
            }
        }

        private static int CoastEdgeVariantCount(GroundBorderEdges edges)
        {
            switch (edges)
            {
                case GroundBorderEdges.West:
                case GroundBorderEdges.North:
                case GroundBorderEdges.North | GroundBorderEdges.West:
                case GroundBorderEdges.East:
                case GroundBorderEdges.East | GroundBorderEdges.West:
                case GroundBorderEdges.South:
                case GroundBorderEdges.North | GroundBorderEdges.South:
                    return 3;
                case GroundBorderEdges.West | GroundBorderEdges.South:
                case GroundBorderEdges.East | GroundBorderEdges.South:
                    return 2;
                default:
                    return 1;
            }
        }

        private static int MaskEdgeVariantCount(GroundBorderEdges edges)
        {
            switch (edges)
            {
                case GroundBorderEdges.West:
                case GroundBorderEdges.North:
                case GroundBorderEdges.East:
                case GroundBorderEdges.South:
                case GroundBorderEdges.North | GroundBorderEdges.South | GroundBorderEdges.West:
                case GroundBorderEdges.North | GroundBorderEdges.East | GroundBorderEdges.South:
                    return 2;
                default:
                    return 1;
            }
        }
    }

    public sealed record Tree(WorldMapTerrain Terrain, int Variant) : AssetSpriteKey
    {
        internal const string Category = "tree";

        public int Variant { get; } = Variant.Mod(GetVariantCount(Terrain));

        public override string CanonicalId => $"{Category}/{Token(Terrain)}/{Variant}";

        internal static Tree? Parse(IReadOnlyList<string> tokens)
        {
            if (tokens is not [string terrain, string variant])
            {
                return null;
            }

            WorldMapTerrain parsed = ParseEnum<WorldMapTerrain>(terrain);

            if (parsed == WorldMapTerrain.Water)
            {
                return null;
            }

            return new Tree(parsed, ParseInt(variant));
        }

        public static int GetVariantCount(WorldMapTerrain terrain)
        {
            return terrain switch
            {
                WorldMapTerrain.Human => 24,
                WorldMapTerrain.Undead => 25,
                WorldMapTerrain.Heretic => 23,
                WorldMapTerrain.Dwarf => 23,
                WorldMapTerrain.Elf => 24,
                WorldMapTerrain.Neutral => 20,
                WorldMapTerrain.Water => throw new ArgumentOutOfRangeException(nameof(terrain), terrain, null)
            };
        }
    }

    public sealed record Mountain(WorldRace Race, int Size, int Variant) : AssetSpriteKey
    {
        internal const string Category = "mountain";

        public static readonly ImmutableArray<int> Sizes = [1, 2, 3, 5];

        public int Size { get; } = SnapSize(Size);
        public int Variant { get; } = Variant.Mod(GetVariantCount(SnapSize(Size)));

        public override string CanonicalId => $"{Category}/{Token(Race)}/{Size}/{Variant}";

        internal static Mountain? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string race, string size, string variant] => new Mountain(
                    ParseEnum<WorldRace>(race),
                    ParseInt(size),
                    ParseInt(variant)),
                _ => null
            };
        }

        public static int GetVariantCount(int size)
        {
            return size switch
            {
                1 => 11,
                2 => 10,
                3 => 9,
                5 => 8,
                _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
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

    public sealed record Bag(bool IsWater, int Variant) : AssetSpriteKey
    {
        internal const string Category = "bag";

        private const int LandVariantCount = 8;
        private const int WaterVariantCount = 4;

        public int Variant { get; } = Variant.Mod(GetVariantCount(IsWater));

        public override string CanonicalId => IsWater ? $"{Category}/water/{Variant}" : $"{Category}/land/{Variant}";

        internal static Bag? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                ["water", string variant] => new Bag(IsWater: true, ParseInt(variant)),
                ["land", string variant] => new Bag(IsWater: false, ParseInt(variant)),
                _ => null
            };
        }

        public static int GetVariantCount(bool isWater) => isWater ? WaterVariantCount : LandVariantCount;
    }

    public sealed record Tombstone(bool IsWater) : AssetSpriteKey
    {
        internal const string Category = "tombstone";

        public override string CanonicalId => IsWater ? $"{Category}/water" : $"{Category}/land";

        internal static Tombstone? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                ["water"] => new Tombstone(IsWater: true),
                ["land"] => new Tombstone(IsWater: false),
                _ => null
            };
        }
    }

    public sealed record CapitalBackground(WorldRace Race, int Variant) : AssetSpriteKey
    {
        internal const string Category = "capital-background";

        public const int VariantCount = 3;

        public int Variant { get; } = Variant.Mod(VariantCount);

        public override string CanonicalId => $"{Category}/{Token(Race)}/{Variant}";

        internal static CapitalBackground? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string race, string variant] => new CapitalBackground(
                    ParseEnum<WorldRace>(race),
                    ParseInt(variant)),
                _ => null
            };
        }
    }

    public sealed record MainMenuBackground : AssetSpriteKey
    {
        internal const string Category = "main-menu-background";

        public override string CanonicalId => Category;
    }

    public sealed record MainMenuBackgroundV2 : AssetSpriteKey
    {
        internal const string Category = "main-menu-background-v2";

        public override string CanonicalId => Category;
    }

    public sealed record RaceSelectionBackground(WorldRace Race) : AssetSpriteKey
    {
        internal const string Category = "race-selection-background";

        public override string CanonicalId => $"{Category}/{Token(Race)}";

        internal static RaceSelectionBackground? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string race] => new RaceSelectionBackground(ParseEnum<WorldRace>(race)),
                _ => null
            };
        }
    }

    public abstract record BattleLocation
    {
        public sealed record Terrain(WorldMapTerrain Value) : BattleLocation;

        public sealed record Boat : BattleLocation;

        public sealed record City : BattleLocation;

        public sealed record Ruin : BattleLocation;

        public sealed record Capital(WorldRace Race) : BattleLocation;
    }

    public sealed record BattleBackground(BattleLocation Location, int Variant) : AssetSpriteKey
    {
        internal const string Category = "battle-background";

        public int Variant { get; } = Variant.Mod(GetVariantCount(Location));

        public override string CanonicalId => Location switch
        {
            BattleLocation.Terrain terrain => $"{Category}/terrain/{Token(terrain.Value)}/{Variant}",
            BattleLocation.Boat => $"{Category}/boat/{Variant}",
            BattleLocation.City => $"{Category}/city/{Variant}",
            BattleLocation.Ruin => $"{Category}/ruin/{Variant}",
            BattleLocation.Capital capital => $"{Category}/capital/{Token(capital.Race)}",
            _ => throw new ArgumentOutOfRangeException(nameof(Location), Location, null)
        };

        internal static BattleBackground? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                ["terrain", string terrain, string variant] => new BattleBackground(
                    new BattleLocation.Terrain(ParseEnum<WorldMapTerrain>(terrain)), ParseInt(variant)),
                ["boat", string variant] => new BattleBackground(new BattleLocation.Boat(), ParseInt(variant)),
                ["city", string variant] => new BattleBackground(new BattleLocation.City(), ParseInt(variant)),
                ["ruin", string variant] => new BattleBackground(new BattleLocation.Ruin(), ParseInt(variant)),
                ["capital", string race] => new BattleBackground(new BattleLocation.Capital(ParseEnum<WorldRace>(race)), 0),
                _ => null
            };
        }

        public static IEnumerable<BattleBackground> ListAll()
        {
            foreach (BattleLocation location in ListLocations())
            {
                for (int variant = 0; variant < GetVariantCount(location); variant++)
                {
                    yield return new BattleBackground(location, variant);
                }
            }
        }

        public static int GetVariantCount(BattleLocation location)
        {
            return location switch
            {
                BattleLocation.Terrain terrain => terrain.Value switch
                {
                    WorldMapTerrain.Human => 2,
                    WorldMapTerrain.Undead => 2,
                    WorldMapTerrain.Heretic => 3,
                    WorldMapTerrain.Dwarf => 2,
                    WorldMapTerrain.Neutral => 4,
                    WorldMapTerrain.Elf => 2,
                    WorldMapTerrain.Water => 1
                },
                BattleLocation.Boat => 2,
                BattleLocation.City => 5,
                BattleLocation.Ruin => 3,
                BattleLocation.Capital => 1,
                _ => throw new ArgumentOutOfRangeException(nameof(location), location, null)
            };
        }

        private static IEnumerable<BattleLocation> ListLocations()
        {
            foreach (WorldMapTerrain terrain in Enum.GetValues<WorldMapTerrain>())
            {
                yield return new BattleLocation.Terrain(terrain);
            }

            yield return new BattleLocation.Boat();
            yield return new BattleLocation.City();
            yield return new BattleLocation.Ruin();

            foreach (WorldRace race in Enum.GetValues<WorldRace>())
            {
                if (race != WorldRace.Neutral)
                {
                    yield return new BattleLocation.Capital(race);
                }
            }
        }
    }

    public sealed record BattleCorpse(bool IsLarge, int Variant) : AssetSpriteKey
    {
        internal const string Category = "battle-corpse";

        public const int VariantCount = 2;

        public int Variant { get; } = Variant.Mod(VariantCount);

        public override string CanonicalId => IsLarge ? $"{Category}/large/{Variant}" : $"{Category}/small/{Variant}";

        internal static BattleCorpse? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                ["large", string variant] => new BattleCorpse(IsLarge: true, ParseInt(variant)),
                ["small", string variant] => new BattleCorpse(IsLarge: false, ParseInt(variant)),
                _ => null
            };
        }
    }

    public sealed record UnitFace(UnitDefinitionId DefinitionId) : AssetSpriteKey
    {
        internal const string Category = "unit-face";

        public override string CanonicalId => $"{Category}/{Token(DefinitionId)}";

        internal static UnitFace? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string unitId] => new UnitFace(new UnitDefinitionId(unitId)),
                _ => null
            };
        }
    }

    public sealed record UnitPortrait(UnitDefinitionId DefinitionId) : AssetSpriteKey
    {
        internal const string Category = "unit-portrait";

        public override string CanonicalId => $"{Category}/{Token(DefinitionId)}";

        internal static UnitPortrait? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string unitId] => new UnitPortrait(new UnitDefinitionId(unitId)),
                _ => null
            };
        }
    }

    public sealed record UnitEventPortrait(ScenarioEventPortraitId PortraitId) : AssetSpriteKey
    {
        internal const string Category = "unit-event-portrait";

        public override string CanonicalId => $"{Category}/{Token(PortraitId)}";

        internal static UnitEventPortrait? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string portraitId] => new UnitEventPortrait(new ScenarioEventPortraitId(portraitId)),
                _ => null
            };
        }
    }

    public sealed record SpellIcon(WorldSpellDefinitionId SpellId) : AssetSpriteKey
    {
        internal const string Category = "spell-icon";

        public override string CanonicalId => $"{Category}/{Token(SpellId)}";

        internal static SpellIcon? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string spellId] => new SpellIcon(new WorldSpellDefinitionId(spellId)),
                _ => null
            };
        }
    }

    public sealed record BuildingIcon(WorldPlayerBuildingDefinitionId BuildingId) : AssetSpriteKey
    {
        internal const string Category = "building-icon";

        public override string CanonicalId => $"{Category}/{Token(BuildingId)}";

        internal static BuildingIcon? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string buildingId] => new BuildingIcon(new WorldPlayerBuildingDefinitionId(buildingId)),
                _ => null
            };
        }
    }

    public abstract record ItemIconSource
    {
        public sealed record Item(ItemDefinitionId ItemId) : ItemIconSource;

        public sealed record Scroll(WorldRace Race) : ItemIconSource;
    }

    public sealed record ItemIcon(ItemIconSource Source) : AssetSpriteKey
    {
        internal const string Category = "item-icon";

        public override string CanonicalId => Source switch
        {
            ItemIconSource.Item item => $"{Category}/{Token(item.ItemId)}",
            ItemIconSource.Scroll scroll => $"{Category}/scroll/{Token(scroll.Race)}",
            _ => throw new ArgumentOutOfRangeException(nameof(Source), Source, null)
        };

        internal static ItemIcon? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                ["scroll", string race] => new ItemIcon(new ItemIconSource.Scroll(ParseEnum<WorldRace>(race))),
                [string item] => new ItemIcon(new ItemIconSource.Item(new ItemDefinitionId(item))),
                _ => null
            };
        }
    }

    public sealed record ThiefActionImage(WorldThiefActionType Type, bool IsLarge) : AssetSpriteKey
    {
        internal const string Category = "thief-action-image";

        public override string CanonicalId => $"{Category}/{Token(Type)}/{(IsLarge ? "large" : "small")}";

        internal static ThiefActionImage? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string type, "large"] => new ThiefActionImage(ParseEnum<WorldThiefActionType>(type), IsLarge: true),
                [string type, "small"] => new ThiefActionImage(ParseEnum<WorldThiefActionType>(type), IsLarge: false),
                _ => null
            };
        }
    }

    public sealed record SiteImage(AssetAnimationKey.SiteType Type) : AssetSpriteKey
    {
        internal const string Category = "site-image";

        public override string CanonicalId => $"{Category}/{Token(Type)}";

        internal static SiteImage? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string type] => new SiteImage(ParseEnum<AssetAnimationKey.SiteType>(type)),
                _ => null
            };
        }
    }

    public sealed record CityImage(int Level) : AssetSpriteKey
    {
        internal const string Category = "city-image";

        public int Level { get; } = Math.Clamp(Level, AssetAnimationKey.City.MinLevel, AssetAnimationKey.City.MaxLevel);

        public override string CanonicalId => $"{Category}/{Level}";

        internal static CityImage? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string level] => new CityImage(ParseInt(level)),
                _ => null
            };
        }
    }

    public sealed record CapitalImage(WorldRace Race) : AssetSpriteKey
    {
        internal const string Category = "capital-image";

        public override string CanonicalId => $"{Category}/{Token(Race)}";

        internal static CapitalImage? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string race] => new CapitalImage(ParseEnum<WorldRace>(race)),
                _ => null
            };
        }
    }

    public sealed record LordFace(WorldRace Race, int Variant, bool IsLarge) : AssetSpriteKey
    {
        internal const string Category = "lord-face";

        public int Variant { get; } = Variant.Mod(GetVariantCount(Race));

        public override string CanonicalId => $"{Category}/{Token(Race)}/{Variant}/{(IsLarge ? "large" : "small")}";

        internal static LordFace? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string race, string variant, "large"] => new LordFace(ParseEnum<WorldRace>(race), ParseInt(variant), IsLarge: true),
                [string race, string variant, "small"] => new LordFace(ParseEnum<WorldRace>(race), ParseInt(variant), IsLarge: false),
                _ => null
            };
        }

        public static int GetVariantCount(WorldRace race) => race == WorldRace.Neutral ? 1 : 4;
    }

    public sealed record StackBanner(int Variant) : AssetSpriteKey
    {
        internal const string Category = "stack-banner";

        public const int VariantCount = 15;

        public int Variant { get; } = Variant.Mod(VariantCount);

        public override string CanonicalId => $"{Category}/{Variant}";

        internal static StackBanner? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string variant] => new StackBanner(ParseInt(variant)),
                _ => null
            };
        }
    }

    public sealed record CityShield(WorldRace Race, bool IsCapital) : AssetSpriteKey
    {
        internal const string Category = "city-shield";

        public override string CanonicalId => IsCapital ? $"{Category}/capital/{Token(Race)}" : $"{Category}/city/{Token(Race)}";

        internal static CityShield? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                ["capital", string race] => new CityShield(ParseEnum<WorldRace>(race), IsCapital: true),
                ["city", string race] => new CityShield(ParseEnum<WorldRace>(race), IsCapital: false),
                _ => null
            };
        }
    }

    public sealed record RuinBanner(bool IsLooted) : AssetSpriteKey
    {
        internal const string Category = "ruin-banner";

        public override string CanonicalId => IsLooted ? $"{Category}/looted" : $"{Category}/intact";

        internal static RuinBanner? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                ["looted"] => new RuinBanner(IsLooted: true),
                ["intact"] => new RuinBanner(IsLooted: false),
                _ => null
            };
        }
    }

    public sealed record Rod(WorldRace Race) : AssetSpriteKey
    {
        internal const string Category = "rod";

        public override string CanonicalId => $"{Category}/{Token(Race)}";

        internal static Rod? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                [string race] => new Rod(ParseEnum<WorldRace>(race)),
                _ => null
            };
        }
    }

    public sealed record Road(RoadConnections Connections) : AssetSpriteKey
    {
        internal const string Category = "road";

        public override string CanonicalId => $"{Category}/{ConnectionsToken()}";

        internal static Road? Parse(IReadOnlyList<string> tokens)
        {
            return tokens switch
            {
                ["none"] => new Road(RoadConnections.None),
                [string connections] => new Road(ParseConnections(connections)),
                _ => null
            };
        }

        private static RoadConnections ParseConnections(string token)
        {
            RoadConnections connections = RoadConnections.None;

            foreach (char ch in token)
            {
                connections |= ch switch
                {
                    'n' => RoadConnections.North,
                    'e' => RoadConnections.East,
                    's' => RoadConnections.South,
                    'w' => RoadConnections.West,
                    _ => RoadConnections.None
                };
            }

            return connections;
        }

        private string ConnectionsToken()
        {
            if (Connections == RoadConnections.None)
            {
                return "none";
            }

            string token = "";

            if (Connections.HasFlag(RoadConnections.North))
            {
                token += "n";
            }

            if (Connections.HasFlag(RoadConnections.East))
            {
                token += "e";
            }

            if (Connections.HasFlag(RoadConnections.South))
            {
                token += "s";
            }

            if (Connections.HasFlag(RoadConnections.West))
            {
                token += "w";
            }

            return token;
        }
    }

    [Flags]
    public enum GroundBorderCorners
    {
        None = 0,
        NorthWest = 1 << 0,
        NorthEast = 1 << 1,
        SouthEast = 1 << 2,
        SouthWest = 1 << 3,
        All = NorthWest | NorthEast | SouthEast | SouthWest
    }

    [Flags]
    public enum GroundBorderEdges
    {
        None = 0,
        North = 1 << 0,
        East = 1 << 1,
        South = 1 << 2,
        West = 1 << 3,
        All = North | East | South | West
    }

    public enum GroundBorderSet
    {
        Mask,
        Coast
    }

    [Flags]
    public enum RoadConnections
    {
        None = 0,
        North = 1 << 0,
        East = 1 << 1,
        South = 1 << 2,
        West = 1 << 3,
        All = North | East | South | West
    }

    private static readonly FrozenDictionary<string, Func<IReadOnlyList<string>, AssetSpriteKey?>> Parsers =
        new Dictionary<string, Func<IReadOnlyList<string>, AssetSpriteKey?>>(StringComparer.Ordinal)
        {
            [Ground.Category] = Ground.Parse,
            [GroundBorder.Category] = GroundBorder.Parse,
            [Tree.Category] = Tree.Parse,
            [Mountain.Category] = Mountain.Parse,
            [Bag.Category] = Bag.Parse,
            [Tombstone.Category] = Tombstone.Parse,
            [CapitalBackground.Category] = CapitalBackground.Parse,
            [MainMenuBackground.Category] = _ => new MainMenuBackground(),
            [MainMenuBackgroundV2.Category] = _ => new MainMenuBackgroundV2(),
            [RaceSelectionBackground.Category] = RaceSelectionBackground.Parse,
            [BattleBackground.Category] = BattleBackground.Parse,
            [BattleCorpse.Category] = BattleCorpse.Parse,
            [UnitFace.Category] = UnitFace.Parse,
            [UnitPortrait.Category] = UnitPortrait.Parse,
            [UnitEventPortrait.Category] = UnitEventPortrait.Parse,
            [SpellIcon.Category] = SpellIcon.Parse,
            [BuildingIcon.Category] = BuildingIcon.Parse,
            [ItemIcon.Category] = ItemIcon.Parse,
            [ThiefActionImage.Category] = ThiefActionImage.Parse,
            [SiteImage.Category] = SiteImage.Parse,
            [CityImage.Category] = CityImage.Parse,
            [CapitalImage.Category] = CapitalImage.Parse,
            [LordFace.Category] = LordFace.Parse,
            [StackBanner.Category] = StackBanner.Parse,
            [CityShield.Category] = CityShield.Parse,
            [RuinBanner.Category] = RuinBanner.Parse,
            [Rod.Category] = Rod.Parse,
            [Road.Category] = Road.Parse
        }.ToFrozenDictionary(StringComparer.Ordinal);

    internal static IReadOnlyCollection<string> Categories => Parsers.Keys;

    public new static AssetSpriteKey Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out AssetSpriteKey? key))
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
        [MaybeNullWhen(false)] out AssetSpriteKey result)
    {
        AssetSpriteKey? key = ParseKey(s);

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

    private static AssetSpriteKey? ParseKey(string? id)
    {
        if (id is null)
        {
            return null;
        }

        string[] tokens = id.Split('/');

        if (Parsers.TryGetValue(tokens[0], out Func<IReadOnlyList<string>, AssetSpriteKey?>? parse))
        {
            return parse(tokens[1..]);
        }
        else
        {
            return null;
        }
    }
}
