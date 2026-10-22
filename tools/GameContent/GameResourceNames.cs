using Gallean.Engine.Contracts;

namespace GameContent;

internal static class GameResourceNames
{
    public static class Audio
    {
        public static readonly IReadOnlyList<string> MenuMusic = ["MENUTRCK.WAV"];
        public static readonly IReadOnlyList<string> BriefingMusic = ["GENBRIEF.WAV"];

        public static readonly IReadOnlyList<string> BattleMusic = ["BATTLE01.WAV", "BATTLE02.WAV", "BATTLE03.WAV", "BATTLE04.WAV"];

        public static readonly IReadOnlyList<string> WorldMusic =
        [
            "INGAME01.WAV",
            "INGAME02.WAV",
            "INGAME03.WAV",
            "INGAME04.WAV",
            "INGAME05.WAV",
            "INGAME06.WAV",
            "INGAME07.WAV",
            "INGAME08.WAV",
            "INGAME09.WAV",
            "INGAME10.WAV",
            "INGAME11.WAV"
        ];

        public static readonly IReadOnlyList<string> AmbientMusic =
        [
            "AMB01.WAV",
            "AMB02.WAV",
            "AMB03.WAV",
            "AMB04.WAV",
            "AMB05.WAV",
            "AMB06.WAV",
            "AMB07.WAV",
            "AMB08.WAV",
            "AMB09.WAV",
            "AMB10.WAV",
            "AMB11.WAV"
        ];
        public static readonly IReadOnlyList<string> StackCreateSounds = ["CREATSTK"];
        public static readonly IReadOnlyList<string> StackDestroySounds = ["AUNN7778"];
        public static readonly IReadOnlyList<string> StackSummonDestroySounds = ["AUNN7788"];
        public static readonly IReadOnlyList<string> StackBoatMoveSounds = ["BOATSND"];
        public static readonly IReadOnlyList<string> InterfaceOpenSounds = ["OPENINTR"];
        public static readonly IReadOnlyList<string> BeepSounds = ["BEEP"];
        public static readonly IReadOnlyList<string> BookOpenSounds = ["OPENBOOK"];
        public static readonly IReadOnlyList<string> BookCloseSounds = ["CLOSBOOK"];
        public static readonly IReadOnlyList<string> TurnStartSounds = ["BOTREPRT"];
        public static readonly IReadOnlyList<string> WorldBattleStartSounds = ["SBATTLE"];
        public static readonly IReadOnlyList<string> OccupySounds = ["OCCUPY"];
        public static readonly IReadOnlyList<string> RuinEnterSounds = ["ENTRRUIN"];
        public static readonly IReadOnlyList<string> CityEnterSounds = ["ENTRCITY"];
        public static readonly IReadOnlyList<string> CityExitSounds = ["EXITCITY"];
        public static readonly IReadOnlyList<string> RuinLootSounds = ["LOOTRUIN"];
        public static readonly IReadOnlyList<string> BuildingConstructSounds = ["BUILDING"];
        public static readonly IReadOnlyList<string> CityGrowSounds = ["CITYGROW"];
        public static readonly IReadOnlyList<string> CityRiotEndSounds = ["ENDRIOT"];
        public static readonly IReadOnlyList<string> BagPickUpSounds = ["TAKEBAG"];
        public static readonly IReadOnlyList<string> UnitPurchaseSounds = ["ENROLL"];
        public static readonly IReadOnlyList<string> SitePurchaseSounds = ["BUYITEM"];
        public static readonly IReadOnlyList<string> SiteEnterSounds = ["ENTRSITE"];
        public static readonly IReadOnlyList<string> ItemUseSounds = ["USEITEM"];
        public static readonly IReadOnlyList<string> SpellResearchSounds = ["SPELLDIS"];
        public static readonly IReadOnlyList<string> ThiefActionSounds = ["SNOBLE"];
        public static readonly IReadOnlyList<string> BattleHealSounds = ["HEAL"];
        public static readonly IReadOnlyList<string> BattleDeathSounds = ["DEATH"];
        public static readonly IReadOnlyList<string> AmbientRuinSounds = ["RUIN1", "RUIN2", "RUIN3", "RUIN4"];

        public static IReadOnlyList<string> VictoryMusic(string name)
        {
            return [$"{name}.WAV"];
        }

        public static string BriefingVoice(int number)
        {
            return $"BRIEFEL{number}.WAV";
        }

        public static string BriefingVideo(string name)
        {
            return $"{name}.BIK";
        }

        public static IReadOnlyList<string> StackUnitResponseSounds(UnitDefinitionId unitId)
        {
            string prefix = unitId.Value.ToUpperInvariant();

            return [$"{prefix}ACK1", $"{prefix}ACK2", $"{prefix}ACK3", $"{prefix}ACK4"];
        }

        public static IReadOnlyList<string> StackUnitAttackSounds(UnitDefinitionId unitId)
        {
            string prefix = unitId.Value.ToUpperInvariant();

            return [$"{prefix}ATTK"];
        }

        public static IReadOnlyList<string> StackUnitIdleSounds(UnitDefinitionId unitId)
        {
            string prefix = unitId.Value.ToUpperInvariant();

            return [$"{prefix}IDLE1", $"{prefix}IDLE2"];
        }

        public static IReadOnlyList<string> StackUnitDeathSounds(UnitDefinitionId unitId)
        {
            string prefix = unitId.Value.ToUpperInvariant();

            return [$"{prefix}KILL"];
        }

        public static IReadOnlyList<string> CapitalMusic(WorldRace race)
        {
            return race switch
            {
                WorldRace.Human => ["HUMNTRK1.WAV", "HUMNTRK2.WAV", "HUMNTRK3.WAV"],
                WorldRace.Undead => ["UNDETRK1.WAV", "UNDETRK2.WAV", "UNDETRK3.WAV"],
                WorldRace.Heretic => ["HERETRK1.WAV", "HERETRK2.WAV", "HERETRK3.WAV"],
                WorldRace.Dwarf => ["DWRFTRK1.WAV", "DWRFTRK2.WAV", "DWRFTRK3.WAV"],
                WorldRace.Neutral => [],
                WorldRace.Elf => ["ELFTRK1.WAV", "ELFTRK2.WAV", "ELFTRK3.WAV"]
            };
        }

        public static IReadOnlyList<string> BattleStatusSounds(AssetSoundKey.BattleEffect.StatusType status)
        {
            return status switch
            {
                AssetSoundKey.BattleEffect.StatusType.Poison => ["POISON"],
                AssetSoundKey.BattleEffect.StatusType.Frostbite => ["FRSTBITE"],
                AssetSoundKey.BattleEffect.StatusType.Blister => ["BLISTER"],
                AssetSoundKey.BattleEffect.StatusType.ShatterArmor => ["SHATTER"],
                AssetSoundKey.BattleEffect.StatusType.Transform => ["UNTRANSF"],
                AssetSoundKey.BattleEffect.StatusType.Unsummon => ["UNSUMMON"]
            };
        }

        public static IReadOnlyList<string> BattlePotionSounds(AssetSoundKey.BattleEffect.PotionType potion)
        {
            return potion switch
            {
                AssetSoundKey.BattleEffect.PotionType.Boost => ["PBOOST"],
                AssetSoundKey.BattleEffect.PotionType.Heal => ["PHEAL"],
                AssetSoundKey.BattleEffect.PotionType.Revive => ["PREVIVE"]
            };
        }

        public static IReadOnlyList<string> AmbientSounds(AssetSoundKey.AmbientSoundType type)
        {
            return type switch
            {
                AssetSoundKey.AmbientSoundType.City => ["CITY"],
                AssetSoundKey.AmbientSoundType.Waterfall => ["WATRFALL"],
                AssetSoundKey.AmbientSoundType.Cemetery => ["CEMETARY"],
                AssetSoundKey.AmbientSoundType.Lava => ["LAVA"],
                AssetSoundKey.AmbientSoundType.Torch => ["TORCH"],
                AssetSoundKey.AmbientSoundType.Whirlpool => ["WHIRL"],
                AssetSoundKey.AmbientSoundType.CityInFlames => ["FLAMECTY"],
                AssetSoundKey.AmbientSoundType.GateClosed => ["GATECLSD"],
                AssetSoundKey.AmbientSoundType.GateOpen => ["GATEOPEN"],
                AssetSoundKey.AmbientSoundType.GoldMine => ["GOLDMINE"],
                AssetSoundKey.AmbientSoundType.MagicTower => ["MAGICTW"],
                AssetSoundKey.AmbientSoundType.Windmill => ["WINDMILL"]
            };
        }

        public static IReadOnlyList<string> AmbientCapitalSounds(WorldRace race)
        {
            return race switch
            {
                WorldRace.Human => ["CAPHU"],
                WorldRace.Undead => ["CAPUN"],
                WorldRace.Heretic => ["CAPHE"],
                WorldRace.Dwarf => ["CAPDW"],
                WorldRace.Elf => ["CAPEL"],
                WorldRace.Neutral => []
            };
        }
    }

    public static class Battle
    {
        public static string Unit(string graphicsId, AssetAnimationKey.BattleUnitPose pose, AssetAnimationKey.BattleLayer layer, BattleSide side)
        {
            return $"{graphicsId}{PoseCode(pose)}{LayerCode(layer)}{DirectionCode(side)}00";
        }

        public static string AttackEffect(
            string graphicsId,
            AssetAnimationKey.BattleEffect.AttackType type,
            AssetAnimationKey.BattleLayer layer,
            AssetAnimationKey.BattleEffect.AttackSide side,
            int variant)
        {
            return $"{graphicsId}{AttackEffectTypeCode(type)}{LayerCode(layer)}{AttackEffectSideCode(side)}{variant:D2}";
        }

        public static string Corpse(bool isLarge, int variant)
        {
            string size = isLarge ? "L" : "S";

            return $"DEAD_HUMAN_{size}A{variant:D2}";
        }

        public static string? DeathEffect(UnitDeathAnimation animation, bool isLarge)
        {
            if (isLarge)
            {
                return LargeDeathEffect(animation);
            }
            else
            {
                return SmallDeathEffect(animation);
            }
        }

        public static int DeathEffectRevealFrame(string name)
        {
            return int.Parse(name.AsSpan(name.Length - 2));
        }

        public static string StatusEffect(AssetAnimationKey.BattleEffect.StatusType type, bool isLarge)
        {
            string size = isLarge ? "L" : "S";

            return $"{StatusEffectCode(type)}{size}";
        }

        public static string HealEffect()
        {
            return TouchEffect("HEAL");
        }

        public static string BlisterEffect()
        {
            return TouchEffect("BLIS");
        }

        public static string ItemEffect(AssetAnimationKey.BattleEffect.ItemType type)
        {
            return $"{ItemEffectGraphicsId(type)}HMOVA1B00";
        }

        public static string LevelUpEffect(WorldRace race, bool isLarge)
        {
            string size = isLarge ? "L" : "S";

            return $"UPGRADE{LevelUpRaceCode(race)}{size}";
        }

        private static char DirectionCode(BattleSide side)
        {
            return side switch
            {
                BattleSide.Attacker => 'A',
                BattleSide.Defender => 'D'
            };
        }

        private static string LayerCode(AssetAnimationKey.BattleLayer layer)
        {
            return layer switch
            {
                AssetAnimationKey.BattleLayer.Shadow => "S1",
                AssetAnimationKey.BattleLayer.Body => "A1",
                AssetAnimationKey.BattleLayer.Aura => "A2"
            };
        }

        private static string SmallDeathEffect(UnitDeathAnimation animation)
        {
            return animation switch
            {
                UnitDeathAnimation.Human => "DEATH_HUMAN_S13",
                UnitDeathAnimation.Heretic => "DEATH_HERETIC_S13",
                UnitDeathAnimation.Dwarf => "DEATH_DWARF_S15",
                UnitDeathAnimation.Undead => "DEATH_UNDEAD_S15",
                UnitDeathAnimation.Neutral => "DEATH_NEUTRAL_S10",
                UnitDeathAnimation.Dragon => "DEATH_DRAGON_S15",
                UnitDeathAnimation.Ghost => "DEATH_GHOST_S15",
                UnitDeathAnimation.Elf => "DEATH_ELF_S15"
            };
        }

        private static string? LargeDeathEffect(UnitDeathAnimation animation)
        {
            return animation switch
            {
                UnitDeathAnimation.Human => null,
                UnitDeathAnimation.Heretic => null,
                UnitDeathAnimation.Dwarf => null,
                UnitDeathAnimation.Undead => null,
                UnitDeathAnimation.Neutral => null,
                UnitDeathAnimation.Dragon => null,
                UnitDeathAnimation.Ghost => null,
                UnitDeathAnimation.Elf => "DEATH_ELF_L15"
            };
        }

        private static string StatusEffectCode(AssetAnimationKey.BattleEffect.StatusType type)
        {
            return type switch
            {
                AssetAnimationKey.BattleEffect.StatusType.Poison => "POISONANIM",
                AssetAnimationKey.BattleEffect.StatusType.Frostbite => "FROSTBITEANIM",
                AssetAnimationKey.BattleEffect.StatusType.Blister => "BLISTERANIM",
                AssetAnimationKey.BattleEffect.StatusType.Revive => "REVIVEANIM",
                AssetAnimationKey.BattleEffect.StatusType.ReduceLevel => "DRAINLVL",
                AssetAnimationKey.BattleEffect.StatusType.Transform => "TRANSF",
                AssetAnimationKey.BattleEffect.StatusType.Doppelganger => "DOPPEL",
                AssetAnimationKey.BattleEffect.StatusType.Unsummon => "USUMMONANIM",
                AssetAnimationKey.BattleEffect.StatusType.RestoreLevel => "UDRAINLVL",
                AssetAnimationKey.BattleEffect.StatusType.Untransform => "UTRANSF",
                AssetAnimationKey.BattleEffect.StatusType.Undoppelganger => "UDOPPEL"
            };
        }

        private static string ItemEffectGraphicsId(AssetAnimationKey.BattleEffect.ItemType type)
        {
            return type switch
            {
                AssetAnimationKey.BattleEffect.ItemType.Potion => "G000IG1111",
                AssetAnimationKey.BattleEffect.ItemType.Orb => "G000IG2222",
                AssetAnimationKey.BattleEffect.ItemType.Talisman => "G000IG3333"
            };
        }

        private static string TouchEffect(string graphicsId)
        {
            return AttackEffect(
                graphicsId,
                AssetAnimationKey.BattleEffect.AttackType.Touch,
                AssetAnimationKey.BattleLayer.Body,
                AssetAnimationKey.BattleEffect.AttackSide.Both,
                variant: 0);
        }

        private static string AttackEffectTypeCode(AssetAnimationKey.BattleEffect.AttackType type)
        {
            return type switch
            {
                AssetAnimationKey.BattleEffect.AttackType.Hit => "HEFF",
                AssetAnimationKey.BattleEffect.AttackType.Touch => "TUCH"
            };
        }

        private static char AttackEffectSideCode(AssetAnimationKey.BattleEffect.AttackSide side)
        {
            return side switch
            {
                AssetAnimationKey.BattleEffect.AttackSide.Attack => 'A',
                AssetAnimationKey.BattleEffect.AttackSide.Defend => 'D',
                AssetAnimationKey.BattleEffect.AttackSide.Both => 'B'
            };
        }

        private static string LevelUpRaceCode(WorldRace race)
        {
            return race switch
            {
                WorldRace.Human => "HU",
                WorldRace.Undead => "UN",
                WorldRace.Heretic => "HE",
                WorldRace.Dwarf => "DW",
                WorldRace.Neutral => "NE",
                WorldRace.Elf => "EL"
            };
        }

        private static string PoseCode(AssetAnimationKey.BattleUnitPose pose)
        {
            return pose switch
            {
                AssetAnimationKey.BattleUnitPose.Idle => "IDLE",
                AssetAnimationKey.BattleUnitPose.Attack => "HMOV",
                AssetAnimationKey.BattleUnitPose.Hit => "HHIT"
            };
        }
    }

    public static class Cities
    {
        public static string CityImage(int level)
        {
            return $"ALN{level}";
        }

        public static string? CapitalImage(WorldRace race)
        {
            return race switch
            {
                WorldRace.Human => "ALH0",
                WorldRace.Undead => "ALU0",
                WorldRace.Heretic => "ALE0",
                WorldRace.Dwarf => "ALD0",
                WorldRace.Neutral => null,
                WorldRace.Elf => "ALF0"
            };
        }
    }

    public static class Cursors
    {
        public sealed record CursorImage(bool IsBattle, string Name);

        public static CursorImage Image(AssetAnimationKey.CursorType type)
        {
            return type switch
            {
                AssetAnimationKey.CursorType.Default => new CursorImage(IsBattle: false, "DEFAULT"),
                AssetAnimationKey.CursorType.Wait => new CursorImage(IsBattle: false, "WAIT"),
                AssetAnimationKey.CursorType.Alliance => new CursorImage(IsBattle: false, "ALLIANCE"),
                AssetAnimationKey.CursorType.Attack => new CursorImage(IsBattle: false, "BATTLE"),
                AssetAnimationKey.CursorType.Negotiate => new CursorImage(IsBattle: false, "NEGO"),
                AssetAnimationKey.CursorType.EnterCity => new CursorImage(IsBattle: false, "ENTERCITY"),
                AssetAnimationKey.CursorType.Move => new CursorImage(IsBattle: false, "MOVE"),
                AssetAnimationKey.CursorType.SelectUnit => new CursorImage(IsBattle: false, "SELECTUNIT"),
                AssetAnimationKey.CursorType.SelectCity => new CursorImage(IsBattle: false, "SELECTCITY"),
                AssetAnimationKey.CursorType.ViewCity => new CursorImage(IsBattle: false, "VIEWCITY"),
                AssetAnimationKey.CursorType.ManageStack => new CursorImage(IsBattle: false, "MANAGESTK"),
                AssetAnimationKey.CursorType.UseScroll => new CursorImage(IsBattle: false, "SCROLLVAL"),
                AssetAnimationKey.CursorType.UseScrollInvalid => new CursorImage(IsBattle: false, "SCROLLINV"),
                AssetAnimationKey.CursorType.CastSpell => new CursorImage(IsBattle: false, "SPELLVAL"),
                AssetAnimationKey.CursorType.CastSpellInvalid => new CursorImage(IsBattle: false, "SPELLINV"),
                AssetAnimationKey.CursorType.TakeBag => new CursorImage(IsBattle: false, "TAKEBAG"),
                AssetAnimationKey.CursorType.Exchange => new CursorImage(IsBattle: false, "EXCHANGE"),
                AssetAnimationKey.CursorType.Visit => new CursorImage(IsBattle: false, "VISIT"),
                AssetAnimationKey.CursorType.DropBag => new CursorImage(IsBattle: false, "DROPBAG"),
                AssetAnimationKey.CursorType.CreateRod => new CursorImage(IsBattle: false, "DROPRODVAL"),
                AssetAnimationKey.CursorType.CreateRodInvalid => new CursorImage(IsBattle: false, "DROPRODINV"),
                AssetAnimationKey.CursorType.DestroyRod => new CursorImage(IsBattle: false, "BREAKROD"),
                AssetAnimationKey.CursorType.DragInvalid => new CursorImage(IsBattle: false, "NODRAGDROP"),
                AssetAnimationKey.CursorType.Dismiss => new CursorImage(IsBattle: false, "DISMISSVAL"),
                AssetAnimationKey.CursorType.DismissInvalid => new CursorImage(IsBattle: false, "DISMISSINV"),
                AssetAnimationKey.CursorType.Heal => new CursorImage(IsBattle: false, "HEALVAL"),
                AssetAnimationKey.CursorType.HealInvalid => new CursorImage(IsBattle: false, "HEALINV"),
                AssetAnimationKey.CursorType.Revive => new CursorImage(IsBattle: false, "REVIVEVAL"),
                AssetAnimationKey.CursorType.ReviveInvalid => new CursorImage(IsBattle: false, "REVIVEINV"),
                AssetAnimationKey.CursorType.DynamicLevel => new CursorImage(IsBattle: false, "DYNLEVELVAL"),
                AssetAnimationKey.CursorType.DynamicLevelInvalid => new CursorImage(IsBattle: false, "DYNLEVELINV"),
                AssetAnimationKey.CursorType.BattleDefault => new CursorImage(IsBattle: true, "CUDEFAULT"),
                AssetAnimationKey.CursorType.BattleAttack => new CursorImage(IsBattle: true, "CUATTACK"),
                AssetAnimationKey.CursorType.BattleAttackInvalid => new CursorImage(IsBattle: true, "CUATTACKN"),
                AssetAnimationKey.CursorType.BattleHeal => new CursorImage(IsBattle: true, "CUHEAL"),
                AssetAnimationKey.CursorType.BattleHealInvalid => new CursorImage(IsBattle: true, "CUHEALN"),
                AssetAnimationKey.CursorType.BattleTransform => new CursorImage(IsBattle: true, "CUTRANSF"),
                AssetAnimationKey.CursorType.BattleTransformInvalid => new CursorImage(IsBattle: true, "CUTRANSFN")
            };
        }

        public static string Animated(string name)
        {
            return $"{name}A";
        }
    }

    public static class Icons
    {
        public static string Scroll(WorldRace race)
        {
            return race switch
            {
                WorldRace.Human => "SCROLLHU",
                WorldRace.Undead => "SCROLLUN",
                WorldRace.Heretic => "SCROLLHE",
                WorldRace.Dwarf => "SCROLLDW",
                WorldRace.Neutral => "SCROLLNE",
                WorldRace.Elf => "SCROLLEL"
            };
        }
    }

    public static class Interface
    {
        public sealed record ChooseRaceOverlay(string ElementName, string BaseAnimationName);

        public const string MainMenuScene = "DLG_MAIN_MENU";
        public const string ChooseRaceScene = "DLG_CHOOSE_RACE";

        public static readonly IReadOnlyList<ChooseRaceOverlay> ChooseRaceOverlays =
        [
            new("IMG_FIREFLY", "_FIREFLY"),
            new("IMG_WATER", "_WATER")
        ];

        public static string SiteImage(AssetAnimationKey.SiteType type)
        {
            return type switch
            {
                AssetAnimationKey.SiteType.Mage => "DLG_MAGE_IMAGESMAGE",
                AssetAnimationKey.SiteType.Merchant => "DLG_MERCHANT_IMAGESMARCHANT",
                AssetAnimationKey.SiteType.Mercenary => "DLG_MERCENARIES_IMAGESMERCENAIRE",
                AssetAnimationKey.SiteType.Trainer => "DLG_TRAINING_CAMP_IMAGESTRAIN"
            };
        }

        public static string ChooseRaceBackground(WorldRace race)
        {
            return $"DLG_CHOOSE_RACE_GOD{ChooseRaceCode(race)}_BG";
        }

        public static string? ChooseRaceOverlaySuffix(WorldRace race)
        {
            return race switch
            {
                WorldRace.Human => "HUMAN",
                WorldRace.Undead => "UNDEAD",
                WorldRace.Heretic => "HERETIC",
                WorldRace.Dwarf => null,
                WorldRace.Neutral => null,
                WorldRace.Elf => null
            };
        }

        private static string ChooseRaceCode(WorldRace race)
        {
            return race switch
            {
                WorldRace.Human => "HU",
                WorldRace.Undead => "UN",
                WorldRace.Heretic => "HE",
                WorldRace.Dwarf => "DW",
                WorldRace.Neutral => throw new ArgumentOutOfRangeException(nameof(race), race, null),
                WorldRace.Elf => "EL"
            };
        }
    }

    public static class Portraits
    {
        public static string UnitFace(string graphicsId)
        {
            return $"{graphicsId}FACE.PNG";
        }

        public static string UnitPortrait(string graphicsId)
        {
            return $"{graphicsId}FACEB.PNG";
        }

        public static string ThiefActionImage(WorldThiefActionType type, bool isLarge)
        {
            string name = $"G000NA{(int)type:D4}";

            if (isLarge)
            {
                return $"{name}_BIG";
            }
            else
            {
                return name;
            }
        }

        public static string LordFace(WorldRace race, bool isLarge, int variant)
        {
            char size = isLarge ? 'L' : 'S';

            return $"LORD{LordRaceCode(race)}{size}{variant + 1:D2}";
        }

        private static string LordRaceCode(WorldRace race)
        {
            return race switch
            {
                WorldRace.Human => "HU",
                WorldRace.Undead => "UN",
                WorldRace.Heretic => "HE",
                WorldRace.Dwarf => "DW",
                WorldRace.Neutral => "NE",
                WorldRace.Elf => "EL"
            };
        }
    }

    public static class Stacks
    {
        public const string Create = "G000UU9999CREA8";
        public const string Destroy = "G000UU9999DYIN8";
        public const string SummonDestroy = "G000UU9998DYIN8";
        public const string ChangeOwner = "G000UU9999COWNE";
        public const string Battle = "ABATTLE";

        public static string Unit(string graphicsId, AssetAnimationKey.StackUnitPose pose, int direction)
        {
            return $"{graphicsId}{PoseCode(pose)}{direction}";
        }

        public static string UnitShadow(string graphicsId, AssetAnimationKey.StackUnitPose pose, int direction)
        {
            return $"{graphicsId}{ShadowPoseCode(pose)}{direction}";
        }

        public static string UnitAura(string graphicsId, AssetAnimationKey.StackUnitPose pose, int direction)
        {
            return $"{graphicsId}{AuraPoseCode(pose)}{direction}";
        }

        public static string Boat(int variant, AssetAnimationKey.StackUnitPose pose, int direction)
        {
            return $"G000RR000{variant}{BoatPoseCode(pose)}{direction}";
        }

        public static string BoatShadow(int variant, AssetAnimationKey.StackUnitPose pose, int direction)
        {
            return $"G000RR000{variant}{BoatShadowPoseCode(pose)}{direction}";
        }

        private static string BoatPoseCode(AssetAnimationKey.StackUnitPose pose)
        {
            return pose switch
            {
                AssetAnimationKey.StackUnitPose.Move => "BTMV",
                AssetAnimationKey.StackUnitPose.Stop => "BOAT"
            };
        }

        private static string BoatShadowPoseCode(AssetAnimationKey.StackUnitPose pose)
        {
            return pose switch
            {
                AssetAnimationKey.StackUnitPose.Move => "SBTM",
                AssetAnimationKey.StackUnitPose.Stop => "SBOA"
            };
        }

        private static string PoseCode(AssetAnimationKey.StackUnitPose pose)
        {
            return pose switch
            {
                AssetAnimationKey.StackUnitPose.Move => "MOVE",
                AssetAnimationKey.StackUnitPose.Stop => "STOP"
            };
        }

        private static string ShadowPoseCode(AssetAnimationKey.StackUnitPose pose)
        {
            return pose switch
            {
                AssetAnimationKey.StackUnitPose.Move => "SMOV",
                AssetAnimationKey.StackUnitPose.Stop => "SSTO"
            };
        }

        private static string AuraPoseCode(AssetAnimationKey.StackUnitPose pose)
        {
            return pose switch
            {
                AssetAnimationKey.StackUnitPose.Move => "MOV2",
                AssetAnimationKey.StackUnitPose.Stop => "STO2"
            };
        }
    }

    public static class Spells
    {
        private const int NameLength = 15;

        public static string Prefix(WorldSpellDefinitionId spellId)
        {
            return spellId.Value.ToUpperInvariant();
        }

        public static IReadOnlyList<string> CastSounds(WorldSpellDefinitionId spellId)
        {
            return [spellId.Value.ToUpperInvariant()];
        }

        public static bool TryParseEffectName(string name, out AssetAnimationKey.WorldSpellEffectLayer layer, out int sublayer, out int reveal)
        {
            layer = default;
            sublayer = 0;
            reveal = 0;

            if (name.Length != NameLength)
            {
                return false;
            }

            if (!name.StartsWith("G000SS", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!TryParseLayerCode(name[10], out layer))
            {
                return false;
            }

            if (!int.TryParse(name.AsSpan(11, 1), out sublayer))
            {
                return false;
            }

            if (sublayer < 1 || sublayer > AssetAnimationKey.WorldSpellEffect.SublayerCount)
            {
                return false;
            }

            sublayer -= 1;

            return int.TryParse(name.AsSpan(12, 3), out reveal);
        }

        private static bool TryParseLayerCode(char code, out AssetAnimationKey.WorldSpellEffectLayer layer)
        {
            switch (char.ToUpperInvariant(code))
            {
                case 'G':
                    layer = AssetAnimationKey.WorldSpellEffectLayer.Ground;
                    return true;

                case 'U':
                    layer = AssetAnimationKey.WorldSpellEffectLayer.Under;
                    return true;

                case 'F':
                    layer = AssetAnimationKey.WorldSpellEffectLayer.Front;
                    return true;

                case 'O':
                    layer = AssetAnimationKey.WorldSpellEffectLayer.Over;
                    return true;

                case 'T':
                    layer = AssetAnimationKey.WorldSpellEffectLayer.Top;
                    return true;

                default:
                    layer = default;
                    return false;
            }
        }
    }

    public static class Terrain
    {
        public const string WaterAnimation = "WATERBKGND1";

        public static string GroundSheet(WorldMapTerrain terrain, int sheet)
        {
            return $"{GroundPrefix(terrain)}_{sheet:D2}";
        }

        public static string GroundBorder(AssetSpriteKey.GroundBorderSet set, int imageIndex, int variant)
        {
            return $"{GroundBorderCode(set)}_{imageIndex:D2}_{variant:D2}";
        }

        public static string Tree(WorldMapTerrain terrain)
        {
            return terrain switch
            {
                WorldMapTerrain.Human => "HUMANTREE.PNG",
                WorldMapTerrain.Undead => "UNDEADTREE.PNG",
                WorldMapTerrain.Heretic => "HERETICTREE.PNG",
                WorldMapTerrain.Dwarf => "DWARFTREE.PNG",
                WorldMapTerrain.Neutral => "NEUTRALTREE.PNG",
                WorldMapTerrain.Elf => "ELFTREE.PNG",
                WorldMapTerrain.Water => throw new ArgumentOutOfRangeException(nameof(terrain), terrain, null)
            };
        }

        public static string Mountain(WorldRace race, int size, int image)
        {
            return $"MOM{MountainCode(race)}{size:D2}{image:D2}";
        }

        public static string MountainRemove(int size)
        {
            return $"RMVMOUNTAIN{size}";
        }

        public static string Road(int imageIndex)
        {
            return $"ROAD{imageIndex:D2}00";
        }

        private static string MountainCode(WorldRace race)
        {
            return race switch
            {
                WorldRace.Human => "NE",
                WorldRace.Undead => "NE",
                WorldRace.Heretic => "NE",
                WorldRace.Dwarf => "DW",
                WorldRace.Neutral => "NE",
                WorldRace.Elf => "NE"
            };
        }

        private static string GroundPrefix(WorldMapTerrain terrain)
        {
            return terrain switch
            {
                WorldMapTerrain.Human => "HU",
                WorldMapTerrain.Undead => "UN",
                WorldMapTerrain.Heretic => "HE",
                WorldMapTerrain.Dwarf => "DW",
                WorldMapTerrain.Neutral => "NE",
                WorldMapTerrain.Elf => "EL",
                WorldMapTerrain.Water => "BL"
            };
        }

        private static string GroundBorderCode(AssetSpriteKey.GroundBorderSet set)
        {
            return set switch
            {
                AssetSpriteKey.GroundBorderSet.Mask => "NE",
                AssetSpriteKey.GroundBorderSet.Coast => "WA"
            };
        }
    }

    public static class WorldObjects
    {
        public static string Crystal(WorldResourceType resourceType)
        {
            return $"G000CR0000{CrystalCode(resourceType)}";
        }

        public static string City(int level)
        {
            return $"G000FT0000NE{level}";
        }

        public static string CityShadow(int level)
        {
            return $"{City(level)}S";
        }

        public static string Capital(WorldRace race, bool isCaptured)
        {
            return $"G000FT0000{CapitalRaceCode(race)}{(isCaptured ? "C" : "")}0";
        }

        public static string CityShield(WorldRace race, bool isCapital)
        {
            return $"G000RR{RaceIdCode(race)}{(isCapital ? "SHLC" : "SHLV")}8";
        }

        public static string Rod(WorldRace race)
        {
            return $"G000RR{RaceIdCode(race)}RROD8";
        }

        public static string Ruin(bool isLooted, int variant)
        {
            return $"G000RU0000{(isLooted ? 1 : 0)}{variant:D2}";
        }

        public static string Bag(bool isWater, int variant)
        {
            return $"G000BG0000{(isWater ? variant : 100 + variant):000}";
        }

        public static string Tombstone(bool isWater)
        {
            return $"G000TB0000{(isWater ? "W" : "G")}";
        }

        public static string StackBanner(int variant)
        {
            return $"STACK_BANNER_{variant * 100:0000}";
        }

        public static string RuinBanner(bool isLooted)
        {
            return $"RUIN_ICON_{(isLooted ? 1 : 0)}";
        }

        public static string SiteBanner(AssetAnimationKey.SiteType type)
        {
            return $"SITE_ICON_{SiteCode(type)}";
        }

        public static string Site(AssetAnimationKey.SiteType type, int variant)
        {
            return $"G000SI0000{SiteCode(type)}{variant:D2}";
        }

        public static string SiteShadow(AssetAnimationKey.SiteType type, int variant)
        {
            return $"{Site(type, variant)}S";
        }

        public static string Cloud(int variant)
        {
            return $"CLOUDI{4 + variant:D2}";
        }

        private static string SiteCode(AssetAnimationKey.SiteType type)
        {
            return type switch
            {
                AssetAnimationKey.SiteType.Mage => "MAGE",
                AssetAnimationKey.SiteType.Merchant => "MERH",
                AssetAnimationKey.SiteType.Mercenary => "MERC",
                AssetAnimationKey.SiteType.Trainer => "TRAI"
            };
        }

        private static string CrystalCode(WorldResourceType resourceType)
        {
            return resourceType switch
            {
                WorldResourceType.Gold => "GL",
                WorldResourceType.DeathMana => "RG",
                WorldResourceType.RuneMana => "WH",
                WorldResourceType.LifeMana => "YE",
                WorldResourceType.InfernalMana => "RD",
                WorldResourceType.GroveMana => "GR"
            };
        }

        private static string CapitalRaceCode(WorldRace race)
        {
            return race switch
            {
                WorldRace.Human => "HU",
                WorldRace.Undead => "UN",
                WorldRace.Heretic => "HE",
                WorldRace.Dwarf => "DW",
                WorldRace.Neutral => "HU",
                WorldRace.Elf => "EL"
            };
        }

        private static string RaceIdCode(WorldRace race)
        {
            return race switch
            {
                WorldRace.Human => "0000",
                WorldRace.Undead => "0003",
                WorldRace.Heretic => "0002",
                WorldRace.Dwarf => "0001",
                WorldRace.Neutral => "8888",
                WorldRace.Elf => "0005"
            };
        }
    }
}
